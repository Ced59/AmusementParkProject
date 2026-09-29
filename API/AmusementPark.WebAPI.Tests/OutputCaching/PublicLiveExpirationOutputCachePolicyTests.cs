using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.OutputCaching;
using Moq;
using Xunit;

namespace AmusementPark.WebAPI.Tests.OutputCaching;

public sealed class PublicLiveExpirationOutputCachePolicyTests
{
    [Fact]
    public async Task ServeResponseAsync_ShouldApplyLifetimeRemainingAtStorageTime()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey] =
            now.UtcDateTime.AddSeconds(7.9);
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = CreatePolicy(now);

        await policy.CacheRequestAsync(context, CancellationToken.None);
        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(7), context.ResponseExpirationTimeSpan);
        Assert.True(context.AllowCacheStorage);
    }

    [Fact]
    public async Task ServeResponseAsync_WhenLifetimeIsZero_ShouldDisableStorage()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey] =
            now.UtcDateTime.AddMilliseconds(500);
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = CreatePolicy(now);

        await policy.CacheRequestAsync(context, CancellationToken.None);
        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.False(context.AllowCacheStorage);
    }

    [Fact]
    public async Task ServeResponseAsync_WhenNoCurrentObservation_ShouldUseMaximumLifetime()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey] = null;
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = CreatePolicy(now);

        await policy.CacheRequestAsync(context, CancellationToken.None);
        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.Equal(TimeSpan.FromSeconds(30), context.ResponseExpirationTimeSpan);
        Assert.True(context.AllowCacheStorage);
    }

    [Fact]
    public async Task ServeResponseAsync_WhenGenerationChangedDuringRequest_ShouldDisableStorage()
    {
        DateTimeOffset now = new DateTimeOffset(2026, 9, 29, 12, 0, 0, TimeSpan.Zero);
        PublicLiveCacheGeneration generation = new PublicLiveCacheGeneration();
        DefaultHttpContext httpContext = new DefaultHttpContext();
        httpContext.Items[PublicLiveExpirationOutputCachePolicy.FreshnessTransitionItemKey] = null;
        OutputCacheContext context = new OutputCacheContext
        {
            HttpContext = httpContext,
            AllowCacheStorage = true,
        };
        PublicLiveExpirationOutputCachePolicy policy = CreatePolicy(now, generation);
        await policy.CacheRequestAsync(context, CancellationToken.None);
        Assert.Equal(
            "0",
            context.CacheVaryByRules.VaryByValues[
                PublicLiveExpirationOutputCachePolicy.GenerationItemKey]);

        generation.Advance();
        await policy.ServeResponseAsync(context, CancellationToken.None);

        Assert.False(context.AllowCacheStorage);
    }

    private static PublicLiveExpirationOutputCachePolicy CreatePolicy(
        DateTimeOffset now,
        PublicLiveCacheGeneration? generation = null)
    {
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(now);
        return new PublicLiveExpirationOutputCachePolicy(
            timeProvider.Object,
            generation ?? new PublicLiveCacheGeneration());
    }
}
