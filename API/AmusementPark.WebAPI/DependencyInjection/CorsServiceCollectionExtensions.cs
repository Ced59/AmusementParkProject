using System;
using System.Collections.Generic;
using System.Linq;
using AmusementPark.WebAPI.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre la politique CORS de l'API.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    public const string PolicyName = "AllowSpecificOrigin";

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);
        ArgumentNullException.ThrowIfNull(environment);

        CorsSettings corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        string[] allowedOrigins = NormalizeAllowedOrigins(corsSettings.AllowedOrigins, corsSettings.AllowCredentials, environment);
        string[] allowedMethods = NormalizeTokens(corsSettings.AllowedMethods, ["GET", "POST", "PUT", "PATCH", "DELETE", "OPTIONS"]);
        string[] allowedHeaders = NormalizeTokens(corsSettings.AllowedHeaders, ["Authorization", "Content-Type", "Accept-Language", "X-Requested-With"]);
        string[] exposedHeaders = NormalizeTokens(corsSettings.ExposedHeaders, Array.Empty<string>());

        CorsSettings normalizedSettings = new CorsSettings
        {
            AllowedOrigins = allowedOrigins,
            AllowedMethods = allowedMethods,
            AllowedHeaders = allowedHeaders,
            ExposedHeaders = exposedHeaders,
            AllowCredentials = corsSettings.AllowCredentials,
        };

        services.AddSingleton(normalizedSettings);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policyBuilder =>
            {
                policyBuilder
                    .WithOrigins(normalizedSettings.AllowedOrigins)
                    .WithMethods(normalizedSettings.AllowedMethods)
                    .WithHeaders(normalizedSettings.AllowedHeaders);

                if (normalizedSettings.ExposedHeaders.Length > 0)
                {
                    policyBuilder.WithExposedHeaders(normalizedSettings.ExposedHeaders);
                }

                if (normalizedSettings.AllowCredentials)
                {
                    policyBuilder.AllowCredentials();
                }
            });
        });

        return services;
    }

    public static IApplicationBuilder UseApiCors(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        return app.UseCors(PolicyName);
    }

    private static string[] NormalizeAllowedOrigins(string[] configuredOrigins, bool allowCredentials, IHostEnvironment environment)
    {
        string[] normalizedOrigins = configuredOrigins
            .Where(static origin => !string.IsNullOrWhiteSpace(origin))
            .Select(NormalizeOrigin)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedOrigins.Length == 0)
        {
            if (environment.IsDevelopment())
            {
                return ["http://localhost:4200"];
            }

            throw new InvalidOperationException("At least one explicit CORS origin must be configured outside Development.");
        }

        if (allowCredentials && normalizedOrigins.Any(static origin => origin == "*"))
        {
            throw new InvalidOperationException("CORS wildcard origins cannot be used when credentials are allowed.");
        }

        if (!environment.IsDevelopment())
        {
            string[] forbiddenProductionOrigins = normalizedOrigins
                .Where(IsLocalOrigin)
                .ToArray();

            if (forbiddenProductionOrigins.Length > 0)
            {
                throw new InvalidOperationException($"Localhost CORS origins are not allowed outside Development: {string.Join(", ", forbiddenProductionOrigins)}.");
            }
        }

        return normalizedOrigins;
    }

    private static string NormalizeOrigin(string configuredOrigin)
    {
        string trimmedOrigin = configuredOrigin.Trim().TrimEnd('/');

        if (trimmedOrigin == "*")
        {
            return trimmedOrigin;
        }

        if (!Uri.TryCreate(trimmedOrigin, UriKind.Absolute, out Uri? uri))
        {
            throw new InvalidOperationException($"CORS origin '{configuredOrigin}' is not a valid absolute URI.");
        }

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"CORS origin '{configuredOrigin}' must use http or https.");
        }

        if ((!string.IsNullOrWhiteSpace(uri.AbsolutePath) && uri.AbsolutePath != "/")
            || !string.IsNullOrWhiteSpace(uri.Query)
            || !string.IsNullOrWhiteSpace(uri.Fragment))
        {
            throw new InvalidOperationException($"CORS origin '{configuredOrigin}' must not include a path, query string or fragment.");
        }

        return uri.GetComponents(UriComponents.SchemeAndServer, UriFormat.Unescaped);
    }

    private static bool IsLocalOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out Uri? uri))
        {
            return false;
        }

        return string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "127.0.0.1", StringComparison.OrdinalIgnoreCase)
            || string.Equals(uri.Host, "::1", StringComparison.OrdinalIgnoreCase);
    }

    private static string[] NormalizeTokens(string[] configuredTokens, IReadOnlyCollection<string> defaultTokens)
    {
        string[] normalizedTokens = configuredTokens
            .Where(static token => !string.IsNullOrWhiteSpace(token))
            .Select(static token => token.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizedTokens.Length > 0)
        {
            return normalizedTokens;
        }

        return defaultTokens.ToArray();
    }
}
