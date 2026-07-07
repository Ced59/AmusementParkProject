using System.Net;
using AmusementPark.WebAPI.RateLimiting;
using Microsoft.AspNetCore.Http;
using Xunit;

namespace AmusementPark.WebAPI.Tests.RateLimiting;

public sealed class InternalSsrRateLimitClassifierTests
{
    [Fact]
    public void IsInternalSsrRequest_WhenHeaderAndPrivateRemoteIp_ShouldReturnTrue()
    {
        DefaultHttpContext context = CreateContext("172.30.31.12");
        context.Request.Headers[InternalSsrRateLimitClassifier.HeaderName] = "1";

        bool result = InternalSsrRateLimitClassifier.IsInternalSsrRequest(context);

        Assert.True(result);
    }

    [Fact]
    public void IsInternalSsrRequest_WhenForwardedHeadersMovedPrivateProxyToOriginalFor_ShouldReturnTrue()
    {
        DefaultHttpContext context = CreateContext("66.249.66.1");
        context.Request.Headers[InternalSsrRateLimitClassifier.HeaderName] = "1";
        context.Request.Headers["X-Original-For"] = "172.30.31.12";

        bool result = InternalSsrRateLimitClassifier.IsInternalSsrRequest(context);

        Assert.True(result);
    }

    [Fact]
    public void IsInternalSsrRequest_WhenHeaderIsMissing_ShouldReturnFalse()
    {
        DefaultHttpContext context = CreateContext("172.30.31.12");
        context.Request.Headers["X-Original-For"] = "172.30.31.12";

        bool result = InternalSsrRateLimitClassifier.IsInternalSsrRequest(context);

        Assert.False(result);
    }

    [Fact]
    public void IsInternalSsrRequest_WhenOnlyForwardedForCanBeSpoofed_ShouldReturnFalse()
    {
        DefaultHttpContext context = CreateContext("66.249.66.1");
        context.Request.Headers[InternalSsrRateLimitClassifier.HeaderName] = "1";
        context.Request.Headers["X-Forwarded-For"] = "172.30.31.12";

        bool result = InternalSsrRateLimitClassifier.IsInternalSsrRequest(context);

        Assert.False(result);
    }

    [Fact]
    public void IsInternalSsrRequest_WhenNoTrustedInternalAddressExists_ShouldReturnFalse()
    {
        DefaultHttpContext context = CreateContext("66.249.66.1");
        context.Request.Headers[InternalSsrRateLimitClassifier.HeaderName] = "1";
        context.Request.Headers["X-Original-For"] = "8.8.8.8";

        bool result = InternalSsrRateLimitClassifier.IsInternalSsrRequest(context);

        Assert.False(result);
    }

    private static DefaultHttpContext CreateContext(string remoteIp)
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse(remoteIp);
        return context;
    }
}
