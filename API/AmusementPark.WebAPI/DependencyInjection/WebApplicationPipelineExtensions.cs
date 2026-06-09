using System;
using AmusementPark.WebAPI.Diagnostics;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

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
        app.UseResponseCompression();
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
                    logger.LogError(
                        exception,
                        "Unhandled API exception for {Method} {Path}. TraceId: {TraceId}.",
                        context.Request.Method,
                        context.Request.Path,
                        context.TraceIdentifier);
                }

                ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
                    context,
                    StatusCodes.Status500InternalServerError,
                    ApiProblemDetailsFactory.GetDefaultTitle(StatusCodes.Status500InternalServerError),
                    ApiProblemDetailsFactory.GetDefaultDetail(StatusCodes.Status500InternalServerError),
                    "unexpected.error");

                await ApiProblemDetailsFactory.WriteAsync(context, problemDetails);
            });
        });

        app.UseStatusCodePages(async statusCodeContext =>
        {
            HttpContext context = statusCodeContext.HttpContext;
            int statusCode = context.Response.StatusCode;

            if (statusCode < StatusCodes.Status400BadRequest || context.Response.HasStarted)
            {
                return;
            }

            if (!string.IsNullOrWhiteSpace(context.Response.ContentType) || context.Response.ContentLength.HasValue)
            {
                return;
            }

            ProblemDetails problemDetails = ApiProblemDetailsFactory.Create(
                context,
                statusCode,
                ApiProblemDetailsFactory.GetDefaultTitle(statusCode),
                ApiProblemDetailsFactory.GetDefaultDetail(statusCode),
                $"http.{statusCode}");

            await ApiProblemDetailsFactory.WriteAsync(context, problemDetails);
        });

        app.UseRouting();
        app.UseApiCors();
        app.UseApiRateLimiting();
        app.UseApiAuthenticationRateLimiting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseMiddleware<ApiPerformanceLoggingMiddleware>();
        app.UseOutputCache();
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
