using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AmusementPark.WebAPI.Diagnostics;

/// <summary>
/// Journalise uniquement les requêtes API lentes ou en erreur pour identifier les hot paths CPU.
/// </summary>
public sealed class ApiPerformanceLoggingMiddleware
{
    private readonly RequestDelegate next;
    private readonly ILogger<ApiPerformanceLoggingMiddleware> logger;
    private readonly ApiPerformanceLoggingOptions options;

    public ApiPerformanceLoggingMiddleware(
        RequestDelegate next,
        ILogger<ApiPerformanceLoggingMiddleware> logger,
        IOptions<ApiPerformanceLoggingOptions> options)
    {
        this.next = next;
        this.logger = logger;
        this.options = options.Value;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!this.options.Enabled || this.IsExcludedPath(context.Request.Path))
        {
            await this.next(context);
            return;
        }

        long startedAt = Stopwatch.GetTimestamp();

        try
        {
            await this.next(context);
        }
        finally
        {
            double elapsedMilliseconds = this.GetElapsedMilliseconds(startedAt);
            int statusCode = context.Response.StatusCode;

            if (this.ShouldLog(elapsedMilliseconds, statusCode))
            {
                this.LogRequest(context, elapsedMilliseconds, statusCode);
            }
        }
    }

    private void LogRequest(HttpContext context, double elapsedMilliseconds, int statusCode)
    {
        bool isSlow = elapsedMilliseconds >= Math.Max(1, this.options.SlowRequestThresholdMilliseconds);
        bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;
        string routeTemplate = this.GetRouteTemplate(context);
        ApiPerformanceOutcome outcome = ApiPerformanceOutcomeClassifier.Classify(statusCode);
        string statusFamily = ApiPerformanceOutcomeClassifier.GetStatusFamily(statusCode);
        double roundedElapsedMilliseconds = Math.Round(elapsedMilliseconds, 2);

        if (outcome == ApiPerformanceOutcome.ServerError)
        {
            this.logger.LogError(
                ApiPerformanceLogEvents.ServerError,
                "API request {Method} {RouteTemplate} responded {StatusCode} ({StatusFamily}, {Outcome}) in {ElapsedMilliseconds} ms. Authenticated={IsAuthenticated}, TraceId={TraceId}.",
                context.Request.Method,
                routeTemplate,
                statusCode,
                statusFamily,
                outcome,
                roundedElapsedMilliseconds,
                isAuthenticated,
                context.TraceIdentifier);
            return;
        }

        if (isSlow)
        {
            this.logger.LogWarning(
                ApiPerformanceLogEvents.SlowRequest,
                "Slow API request {Method} {RouteTemplate} responded {StatusCode} ({StatusFamily}, {Outcome}) in {ElapsedMilliseconds} ms. Authenticated={IsAuthenticated}, TraceId={TraceId}.",
                context.Request.Method,
                routeTemplate,
                statusCode,
                statusFamily,
                outcome,
                roundedElapsedMilliseconds,
                isAuthenticated,
                context.TraceIdentifier);
            return;
        }

        this.logger.LogInformation(
            ApiPerformanceLogEvents.RequestCompleted,
            "API request {Method} {RouteTemplate} responded {StatusCode} ({StatusFamily}, {Outcome}) in {ElapsedMilliseconds} ms. Authenticated={IsAuthenticated}, TraceId={TraceId}.",
            context.Request.Method,
            routeTemplate,
            statusCode,
            statusFamily,
            outcome,
            roundedElapsedMilliseconds,
            isAuthenticated,
            context.TraceIdentifier);
    }

    private string GetRouteTemplate(HttpContext context)
    {
        RouteEndpoint? routeEndpoint = context.GetEndpoint() as RouteEndpoint;
        string? rawPattern = routeEndpoint?.RoutePattern.RawText;

        if (!string.IsNullOrWhiteSpace(rawPattern))
        {
            return rawPattern;
        }

        return SensitiveRequestPathSanitizer.Sanitize(context.Request.Path);
    }

    private bool ShouldLog(double elapsedMilliseconds, int statusCode)
    {
        if (this.options.LogAllRequests)
        {
            return true;
        }

        if (elapsedMilliseconds >= Math.Max(1, this.options.SlowRequestThresholdMilliseconds))
        {
            return true;
        }

        return statusCode >= Math.Max(400, this.options.AlwaysLogStatusCodeAtLeast);
    }

    private bool IsExcludedPath(PathString path)
    {
        foreach (string prefix in this.options.ExcludedPathPrefixes)
        {
            if (string.IsNullOrWhiteSpace(prefix))
            {
                continue;
            }

            PathString excludedPrefix = new PathString(prefix.Trim());
            if (path.StartsWithSegments(excludedPrefix, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private double GetElapsedMilliseconds(long startedAt)
    {
        long elapsedTicks = Stopwatch.GetTimestamp() - startedAt;
        return elapsedTicks * 1000.0 / Stopwatch.Frequency;
    }
}
