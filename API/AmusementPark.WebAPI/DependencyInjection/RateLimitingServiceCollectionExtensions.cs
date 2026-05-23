using System;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.RateLimiting;
using AmusementPark.WebAPI.Configuration;
using AmusementPark.WebAPI.RateLimiting;
using AspNetCoreRateLimit;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre le rate limiting global IP et les policies ciblées de l'API.
/// </summary>
public static class RateLimitingServiceCollectionExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.AddMemoryCache();
        services.AddSingleton<IProcessingStrategy, AsyncKeyLockProcessingStrategy>();
        services.Configure<IpRateLimitOptions>(configuration.GetSection("IpRateLimiting"));
        services.AddSingleton<IIpPolicyStore, MemoryCacheIpPolicyStore>();
        services.AddSingleton<IRateLimitCounterStore, MemoryCacheRateLimitCounterStore>();
        services.AddSingleton<IRateLimitConfiguration, RateLimitConfiguration>();
        services.AddHttpContextAccessor();

        AuthenticationRateLimitingSettings authenticationSettings = configuration
            .GetSection(AuthenticationRateLimitingSettings.ConfigurationSectionName)
            .Get<AuthenticationRateLimitingSettings>() ?? new AuthenticationRateLimitingSettings();

        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = static (context, cancellationToken) =>
                new ValueTask(WriteRateLimitRejectionAsync(context, cancellationToken));

            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthLogin, authenticationSettings.Login);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthExternalLogin, authenticationSettings.ExternalLogin);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthRefresh, authenticationSettings.RefreshToken);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthRegistration, authenticationSettings.Registration);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthEmailChallenge, authenticationSettings.EmailChallenge);
            AddFixedWindowIpPolicy(options, RateLimitPolicyNames.AuthPasswordReset, authenticationSettings.PasswordReset);
        });

        return services;
    }

    public static IApplicationBuilder UseApiRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseIpRateLimiting();
    }

    public static IApplicationBuilder UseApiAuthenticationRateLimiting(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseRateLimiter();
    }


    private static async Task WriteRateLimitRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        HttpResponse response = context.HttpContext.Response;
        response.StatusCode = StatusCodes.Status429TooManyRequests;
        response.ContentType = "application/json";

        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out TimeSpan retryAfter))
        {
            response.Headers["Retry-After"] = Math.Ceiling(retryAfter.TotalSeconds).ToString("0");
        }

        await response.WriteAsJsonAsync(new
        {
            StatusCode = StatusCodes.Status429TooManyRequests,
            Message = "Too many authentication requests. Please retry later.",
            TraceId = context.HttpContext.TraceIdentifier,
        }, cancellationToken);
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
}
