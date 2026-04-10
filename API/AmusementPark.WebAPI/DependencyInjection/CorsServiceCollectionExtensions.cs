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
                if (corsSettings.AllowedOrigins.Length == 0)
                {
                    policyBuilder.AllowAnyHeader().AllowAnyMethod().AllowAnyOrigin();
                    return;
                }

                policyBuilder.WithOrigins(corsSettings.AllowedOrigins)
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
