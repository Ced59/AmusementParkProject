using Microsoft.AspNetCore.Routing;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre les services purement HTTP de l'API.
/// </summary>
public static class HttpApiServiceCollectionExtensions
{
    public static IServiceCollection AddHttpApi(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.Configure<RouteOptions>(static options =>
        {
            options.LowercaseUrls = true;
        });

        services.AddControllers();
        services.AddEndpointsApiExplorer();

        return services;
    }
}
