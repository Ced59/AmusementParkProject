using System;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using System.Linq;

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
        services.AddResponseCompression(options =>
        {
            options.EnableForHttps = true;
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/xml",
                "application/problem+json",
                "application/ld+json"
            });
        });
        services.AddApiOutputCaching();
        services.AddControllers();
        services.AddEndpointsApiExplorer();

        return services;
    }
}
