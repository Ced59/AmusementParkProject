using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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

            if (!this.ShouldLog(elapsedMilliseconds, statusCode))
            {
                return;
            }

            bool isSlow = elapsedMilliseconds >= Math.Max(1, this.options.SlowRequestThresholdMilliseconds);
            bool hasAuthorizationHeader = context.Request.Headers.ContainsKey("Authorization");
            bool hasCookieHeader = context.Request.Headers.ContainsKey("Cookie");
            bool isAuthenticated = context.User.Identity?.IsAuthenticated == true;
            string userAgent = context.Request.Headers["User-Agent"].ToString();
            string queryString = context.Request.QueryString.HasValue ? context.Request.QueryString.Value ?? string.Empty : string.Empty;
            double roundedElapsedMilliseconds = Math.Round(elapsedMilliseconds, 2);

            if (isSlow)
            {
                this.logger.LogWarning(
                    "Slow API request {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMilliseconds} ms. Authenticated={IsAuthenticated}, AuthorizationHeader={HasAuthorizationHeader}, CookieHeader={HasCookieHeader}, UserAgent={UserAgent}, TraceId={TraceId}.",
                    context.Request.Method,
                    context.Request.Path.Value,
                    queryString,
                    statusCode,
                    roundedElapsedMilliseconds,
                    isAuthenticated,
                    hasAuthorizationHeader,
                    hasCookieHeader,
                    userAgent,
                    context.TraceIdentifier);
                return;
            }

            this.logger.LogInformation(
                "API request {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMilliseconds} ms. Authenticated={IsAuthenticated}, AuthorizationHeader={HasAuthorizationHeader}, CookieHeader={HasCookieHeader}, UserAgent={UserAgent}, TraceId={TraceId}.",
                context.Request.Method,
                context.Request.Path.Value,
                queryString,
                statusCode,
                roundedElapsedMilliseconds,
                isAuthenticated,
                hasAuthorizationHeader,
                hasCookieHeader,
                userAgent,
                context.TraceIdentifier);
        }
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
