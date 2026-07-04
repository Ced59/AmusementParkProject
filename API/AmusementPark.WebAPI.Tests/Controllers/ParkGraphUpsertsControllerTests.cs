using System.Reflection;
using AmusementPark.WebAPI.Controllers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace AmusementPark.WebAPI.Tests.Controllers;

public sealed class ParkGraphUpsertsControllerTests
{
    [Fact]
    public void DownloadBulkParkJsonExportJobAsync_ShouldAllowAnonymousTokenDownloadAndDisableResponseCache()
    {
        MethodInfo method = typeof(ParkGraphUpsertsController).GetMethod(nameof(ParkGraphUpsertsController.DownloadBulkParkJsonExportJobAsync))
            ?? throw new InvalidOperationException("ParkGraphUpsertsController.DownloadBulkParkJsonExportJobAsync was not found.");

        Assert.NotNull(method.GetCustomAttribute<AllowAnonymousAttribute>());
        ResponseCacheAttribute responseCacheAttribute = method.GetCustomAttribute<ResponseCacheAttribute>()
            ?? throw new InvalidOperationException("ResponseCacheAttribute was not found.");

        Assert.True(responseCacheAttribute.NoStore);
        Assert.Equal(ResponseCacheLocation.None, responseCacheAttribute.Location);
    }

    [Fact]
    public void BuildBulkExportDownloadUrl_ShouldUseForwardedPrefixWhenProxyStripsApiPrefix()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("amusement-parks.fun");
        httpContext.Request.Headers[ParkGraphUpsertsController.ForwardedPrefixHeaderName] = "/api";

        string result = ParkGraphUpsertsController.BuildBulkExportDownloadUrl(httpContext.Request, "job 1", "token+value");

        Assert.Equal("https://amusement-parks.fun/api/admin/park-graph-upserts/bulk/export-jobs/job%201/download?token=token%2Bvalue", result);
    }

    [Fact]
    public void BuildBulkExportDownloadUrl_ShouldFallbackToPathBaseWhenNoForwardedPrefixExists()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Request.Scheme = "https";
        httpContext.Request.Host = new HostString("localhost:44391");
        httpContext.Request.PathBase = "/backend";

        string result = ParkGraphUpsertsController.BuildBulkExportDownloadUrl(httpContext.Request, "job-1", "token");

        Assert.Equal("https://localhost:44391/backend/admin/park-graph-upserts/bulk/export-jobs/job-1/download?token=token", result);
    }
}
