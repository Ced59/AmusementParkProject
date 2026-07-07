using System;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.RateLimiting;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre le rate limiting global IP et les policies ciblées de l'API.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    private const int InternalSsrPermitLimit = 300;
    private const int InternalSsrWindowSeconds = 1;

    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddHttpContextAccessor();

        AuthenticationRateLimitingSettings authenticationSettings = configuration
            .GetSection(AuthenticationRateLimitingSettings.ConfigurationSectionName)
            .Get<AuthenticationRateLimitingSettings>() ?? new AuthenticationRateLimitingSettings();

        FixedWindowRateLimitSettings globalSettings = GetGlobalRateLimitSettings(configuration);
        FixedWindowRateLimitSettings contactSubmissionSettings = configuration
            .GetSection("RateLimiting:Contact:Submission")
            .Get<FixedWindowRateLimitSettings>() ?? FixedWindowRateLimitSettings.Create(3, 900);
        FixedWindowRateLimitSettings socialShareEventSettings = configuration
            .GetSection("RateLimiting:SocialShare:Events")
            .Get<FixedWindowRateLimitSettings>() ?? FixedWindowRateLimitSettings.Create(60, 60);

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, cancellationToken) =>
                new ValueTask(WriteRateLimitRejectionAsync(context, cancellationToken));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
            {
                if (InternalSsrRateLimitClassifier.IsInternalSsrRequest(context))
                {
                    return RateLimitPartition.GetFixedWindowLimiter(
                        partitionKey: "internal-ssr",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = InternalSsrPermitLimit,
                            Window = TimeSpan.FromSeconds(InternalSsrWindowSeconds),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = InternalSsrPermitLimit,
                            AutoReplenishment = true,
                        });
                }

                return RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: GetRemoteIpPartitionKey(context),
                    factory: _ => CreateFixedWindowOptions(globalSettings));
            });

            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthLogin, authenticationSettings.Login);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthExternalLogin, authenticationSettings.ExternalLogin);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthRefresh, authenticationSettings.RefreshToken);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthRegistration, authenticationSettings.Registration);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthEmailChallenge, authenticationSettings.EmailChallenge);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthPasswordReset, authenticationSettings.PasswordReset);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.ContactSubmission, contactSubmissionSettings);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.SocialShareEvents, socialShareEventSettings);
        });

        return services;
    }

    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app;
    }

    public static IApplicationBuilder UseApiAuthenticationRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRateLimiter();
    }

    private static Task WriteRateLimitRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            context.HttpContext.Response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString("0", CultureInfo.InvariantCulture);
        }

        ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
            context.HttpContext,
            StatusCodes.Status429TooManyRequests,
            ApiProblemDetailsFactory.GetDefaultTitle(StatusCodes.Status429TooManyRequests),
            ApiProblemDetailsFactory.GetDefaultDetail(StatusCodes.Status429TooManyRequests),
            "rate-limit.exceeded");

        return ApiProblemDetailsFactory.WriteAsync(context.HttpContext, problemDetails, cancellationToken);
    }

    private static void AddFixedWindowIpPolicy(RateLimiterOptions options, string policyName, FixedWindowRateLimitSettings settings)
    {
        options.AddPolicy(policyName, context => RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: GetRemoteIpPartitionKey(context),
            factory: _ => CreateFixedWindowOptions(settings)));
    }

    private static FixedWindowRateLimiterOptions CreateFixedWindowOptions(FixedWindowRateLimitSettings settings)
    {
        int permitLimit = Math.Max(1, settings.PermitLimit);
        int windowSeconds = Math.Max(1, settings.WindowSeconds);

        return new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitLimit,
            Window = TimeSpan.FromSeconds(windowSeconds),
            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
            QueueLimit = 0,
            AutoReplenishment = true,
        };
    }

    private static string GetRemoteIpPartitionKey(HttpContext context)
    {
        string remoteIp = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return $"ip:{remoteIp}";
    }

    private static FixedWindowRateLimitSettings GetGlobalRateLimitSettings(IConfiguration configuration)
    {
        IConfigurationSection? firstRule = configuration
            .GetSection("IpRateLimiting:GeneralRules")
            .GetChildren()
            .FirstOrDefault();

        if (firstRule is null)
        {
            return new FixedWindowRateLimitSettings
            {
                PermitLimit = 30,
                WindowSeconds = 1,
            };
        }

        return new FixedWindowRateLimitSettings
        {
            PermitLimit = int.TryParse(firstRule["Limit"], NumberStyles.Integer, CultureInfo.InvariantCulture, out int limit) ? limit : 30,
            WindowSeconds = ParsePeriodSeconds(firstRule["Period"]),
        };
    }

    private static int ParsePeriodSeconds(string? period)
    {
        if (string.IsNullOrWhiteSpace(period))
        {
            return 1;
        }

        string trimmedPeriod = period.Trim();
        char suffix = trimmedPeriod[^1];
        string numericPart = char.IsLetter(suffix) ? trimmedPeriod[..^1] : trimmedPeriod;

        if (!int.TryParse(numericPart, NumberStyles.Integer, CultureInfo.InvariantCulture, out int value) || value <= 0)
        {
            return 1;
        }

        return char.ToLowerInvariant(suffix) switch
        {
            's' => value,
            'm' => value * 60,
            'h' => value * 60 * 60,
            'd' => value * 60 * 60 * 24,
            _ => value,
        };
    }
}
