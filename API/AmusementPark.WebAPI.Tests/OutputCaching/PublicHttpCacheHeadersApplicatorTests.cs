using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.Net.Http.Headers;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PublicHttpCacheHeadersApplicatorTests
{
    [Fact]
    public void Apply_WhenImageResponseAlreadyHasImmutableCache_ShouldKeepExistingCacheControl()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/images/image-1";
        context.Response.StatusCode = StatusCodes.Status200OK;
        context.Response.Headers.CacheControl = "public,max-age=31536000,immutable";
        context.Response.Headers.Vary = HeaderNames.Accept;

        PublicHttpCacheHeadersApplicator.Apply(context);

        Assert.Equal("public,max-age=31536000,immutable", context.Response.Headers.CacheControl.ToString());
        Assert.Equal(HeaderNames.Accept, context.Response.Headers.Vary.ToString());
    }

    [Fact]
    public void Apply_WhenPublicDataHasNoCacheControl_ShouldApplyMatchingRule()
    {
        DefaultHttpContext context = new DefaultHttpContext();
        context.Request.Method = HttpMethods.Get;
        context.Request.Path = "/parks";
        context.Response.StatusCode = StatusCodes.Status200OK;

        PublicHttpCacheHeadersApplicator.Apply(context);

        Assert.Equal("public, max-age=60, s-maxage=300", context.Response.Headers.CacheControl.ToString());
        Assert.Equal(HeaderNames.AcceptLanguage, context.Response.Headers.Vary.ToString());
    }
}
