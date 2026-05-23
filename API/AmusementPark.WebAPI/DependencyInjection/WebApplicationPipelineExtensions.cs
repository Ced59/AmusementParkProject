using System;
using AmusementPark.WebAPI.Diagnostics;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.HttpOverrides;

namespace AmusementPark.WebAPI.DependencyInjection;

/// <summary>
/// Centralise le pipeline HTTP et les endpoints transverses de l'API.
/// </summary>
public static class WebApplicationPipelineExtensions
{
    public static WebApplication UseApiPipeline(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.UseForwardedHeaders();
        app.UseApiContentSecurityPolicy();

        if (!app.Environment.IsDevelopment())
        {
            app.UseHsts();
            app.UseHttpsRedirection();
        }

        app.Use(async (context, next) =>
        {
            context.Response.Headers.TryAdd("X-Content-Type-Options", "nosniff");
            context.Response.Headers.TryAdd("X-Frame-Options", "DENY");
            context.Response.Headers.TryAdd("Referrer-Policy", "strict-origin-when-cross-origin");
            context.Response.Headers.TryAdd("Permissions-Policy", "camera=(), microphone=(), geolocation=()");
            await next();
        });
        app.UseExceptionHandler(static errorApp =>
        {
            errorApp.Run(async context =>
            {
                Exception? exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
                ILogger logger = context.RequestServices
                    .GetRequiredService<ILoggerFactory>()
                    .CreateLogger("AmusementPark.WebAPI.UnhandledException");

                if (exception is not null)
                {
                    logger.LogError(exception, "Unhandled API exception for {Method} {Path}.", context.Request.Method, context.Request.Path);
                }

                context.Response.StatusCode = StatusCodes.Status500InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsJsonAsync(new
                {
                    StatusCode = StatusCodes.Status500InternalServerError,
                    Message = "An unexpected error occurred.",
                });
            });
        });

        app.UseRouting();
        app.UseApiCors();
        app.UseApiRateLimiting();
        app.UseApiAuthenticationRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();
        return app;
    }

    public static WebApplication MapDiagnosticEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapControllers();
        app.MapGet("/health", () => Results.Ok(MigrationDiagnostics.CreateHealthPayload())).AllowAnonymous();

        return app;
    }
}
