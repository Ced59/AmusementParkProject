using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PublicLiveExpirationOutputCachePolicyTests
{
    [Fact]
    public async Task ServeResponseAsync_ShouldApplyControllerLifetime()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.CacheLifetimeItemKey] =
            TimeSpan.FromSeconds(7);
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = new PublicLiveExpirationOutputCachePolicy();

        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(7), context.ResponseExpirationTimeSpan);
        Assert.True(context.AllowCacheStorage);
    }

    [Fact]
    public async Task ServeResponseAsync_WhenLifetimeIsZero_ShouldDisableStorage()
    {
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.CacheLifetimeItemKey] = TimeSpan.Zero;
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = new PublicLiveExpirationOutputCachePolicy();

        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.False(context.AllowCacheStorage);
    }
}
