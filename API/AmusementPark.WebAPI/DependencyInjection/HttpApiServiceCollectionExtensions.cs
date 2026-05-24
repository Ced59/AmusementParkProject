using System;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

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

        services.Configure<ApiBehaviorOptions>(static options =>
        {
            options.InvalidModelStateResponseFactory = context =>
                ApiProblemDetailsFactory.ToObjectResult(
                    ApiProblemDetailsFactory.CreateValidation(context.HttpContext, context.ModelState));
        });

        services.AddProblemDetails();
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        return services;
    }
}
