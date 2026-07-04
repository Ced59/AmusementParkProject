using System;
using System.IO.Compression;
using System.Linq;
using AmusementPark.Application.Features.ParkWeather.Ports;
using AmusementPark.WebAPI.Diagnostics;
using AmusementPark.WebAPI.OutputCaching;
using AmusementPark.WebAPI.Responses;
using AmusementPark.WebAPI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Enregistre les services purement HTTP de l'API.
/// </summary>
public static class HttpApiServiceCollectionExtensions
{
    public static IServiceCollection AddHttpApi(this IServiceCollection services, IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<ApiPerformanceLoggingOptions>(configuration.GetSection(ApiPerformanceLoggingOptions.SectionName));
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
            options.Providers.Add<BrotliCompressionProvider>();
            options.Providers.Add<GzipCompressionProvider>();
            options.MimeTypes = ResponseCompressionDefaults.MimeTypes.Concat(new[]
            {
                "application/json",
                "application/xml",
                "application/problem+json",
                "application/ld+json"
            }).Distinct(StringComparer.OrdinalIgnoreCase);
        });
        services.Configure<BrotliCompressionProviderOptions>(static options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        services.Configure<GzipCompressionProviderOptions>(static options =>
        {
            options.Level = CompressionLevel.Fastest;
        });
        services.AddApiOutputCaching();
        services.AddScoped<ISsrPageCacheInvalidationRequestResolver, SsrPageCacheInvalidationRequestResolver>();
        services.AddScoped<IParkWeatherCacheInvalidator, ParkWeatherPublicCacheInvalidator>();
        services.AddSingleton<IBulkParkGraphExportJobService, BulkParkGraphExportJobService>();
        services.AddControllers(static options =>
        {
            options.Filters.Add<InvalidatePublicCachesFilter>();
        });
        services.AddEndpointsApiExplorer();

        return services;
    }
}
