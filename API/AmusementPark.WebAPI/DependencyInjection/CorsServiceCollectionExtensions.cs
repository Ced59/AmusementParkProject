using AmusementPark.WebAPI.Configuration;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre la politique CORS de l'API.
/// </summary>
public static class CorsServiceCollectionExtensions
{
    public const string PolicyName = "AllowSpecificOrigin";

    public static IServiceCollection AddApiCors(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        CorsSettings corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>() ?? new CorsSettings();
        services.AddSingleton(corsSettings);

        services.AddCors(options =>
        {
            options.AddPolicy(PolicyName, policyBuilder =>
            {
                string[] allowedOrigins = corsSettings.AllowedOrigins
                    .Where(static origin => !string.IsNullOrWhiteSpace(origin))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (allowedOrigins.Length == 0)
                {
                    allowedOrigins = ["http://localhost:4200"];
                }

                policyBuilder.WithOrigins(allowedOrigins)
                    .AllowAnyHeader()
                    .AllowAnyMethod();

                if (corsSettings.AllowCredentials)
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
}
