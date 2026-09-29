using AmusementPark.WebAPI.Diagnostics;
using AmusementPark.WebAPI.Tests.TestDoubles;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Diagnostics;

public sealed class ApiPerformanceLoggingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_ShouldUseRouteTemplateAndExcludeRequestMetadata()
    {
        RecordingLogger<ApiPerformanceLoggingMiddleware> logger = new RecordingLogger<ApiPerformanceLoggingMiddleware>();
        ApiPerformanceLoggingOptions options = new ApiPerformanceLoggingOptions
        {
            LogAllRequests = true,
            SlowRequestThresholdMilliseconds = 60_000
        };
        ApiPerformanceLoggingMiddleware middleware = new ApiPerformanceLoggingMiddleware(
            static context =>
            {
                context.Response.StatusCode = StatusCodes.Status200OK;
                return Task.CompletedTask;
            },
            logger,
            Options.Create(options));
        DefaultHttpContext context = CreateContext("/parks/technical-park-id", "/parks/{parkId}");
        context.Request.QueryString = new QueryString("?token=private-value");
        context.Request.Headers.UserAgent = "private-user-agent";

        await middleware.InvokeAsync(context);

        (LogLevel Level, EventId EventId, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Information, entry.Level);
        Assert.Equal(ApiPerformanceLogEvents.RequestCompleted, entry.EventId);
        Assert.Contains("/parks/{parkId}", entry.Message, StringComparison.Ordinal);
        Assert.Contains("Success", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("technical-park-id", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private-value", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private-user-agent", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_ShouldClassifyServerErrorsWithDedicatedEvent()
    {
        RecordingLogger<ApiPerformanceLoggingMiddleware> logger = new RecordingLogger<ApiPerformanceLoggingMiddleware>();
        ApiPerformanceLoggingMiddleware middleware = new ApiPerformanceLoggingMiddleware(
            static context =>
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                return Task.CompletedTask;
            },
            logger,
            Options.Create(new ApiPerformanceLoggingOptions()));
        DefaultHttpContext context = CreateContext("/parks", "/parks");

        await middleware.InvokeAsync(context);

        (LogLevel Level, EventId EventId, string Message) entry = Assert.Single(logger.Entries);
        Assert.Equal(LogLevel.Error, entry.Level);
        Assert.Equal(ApiPerformanceLogEvents.ServerError, entry.EventId);
        Assert.Contains("5xx", entry.Message, StringComparison.Ordinal);
        Assert.Contains("ServerError", entry.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task InvokeAsync_ShouldKeepSensitiveFallbackPathsRedacted()
    {
        RecordingLogger<ApiPerformanceLoggingMiddleware> logger = new RecordingLogger<ApiPerformanceLoggingMiddleware>();
        ApiPerformanceLoggingOptions options = new ApiPerformanceLoggingOptions
        {
            LogAllRequests = true,
            SlowRequestThresholdMilliseconds = 60_000
        };
        ApiPerformanceLoggingMiddleware middleware = new ApiPerformanceLoggingMiddleware(
            static _ => Task.CompletedTask,
            logger,
            Options.Create(options));
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Path = "/public/trip-invitations/private-token/preview";

        await middleware.InvokeAsync(context);

        (LogLevel Level, EventId EventId, string Message) entry = Assert.Single(logger.Entries);
        Assert.Contains("/public/trip-invitations/[REDACTED]/preview", entry.Message, StringComparison.Ordinal);
        Assert.DoesNotContain("private-token", entry.Message, StringComparison.Ordinal);
    }

    private static DefaultHttpContext CreateContext(string path, string routePattern)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = path;
        RouteEndpoint endpoint = new RouteEndpoint(
            static _ => Task.CompletedTask,
            RoutePatternFactory.Parse(routePattern),
            0,
            EndpointMetadataCollection.Empty,
            routePattern);
        context.SetEndpoint(endpoint);
        return context;
    }
}
