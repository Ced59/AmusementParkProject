using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Services.History;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.History;

public sealed class InMemoryHistoricalParkRolloutGateCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldReuseGateUntilInvalidated()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        using InMemoryHistoricalParkRolloutGateCache cache = new(memoryCache);
        int factoryCalls = 0;
        Func<CancellationToken, Task<HistoricalParkRolloutGate>> factory = _ =>
        {
            factoryCalls++;
            return Task.FromResult(new HistoricalParkRolloutGate(
                2,
                2,
                1,
                new[] { 1998 }));
        };

        HistoricalParkRolloutGate first = await cache.GetOrCreateAsync(
            "park-1",
            "scope-a",
            factory,
            CancellationToken.None);
        HistoricalParkRolloutGate second = await cache.GetOrCreateAsync(
            "park-1",
            "scope-a",
            factory,
            CancellationToken.None);
        cache.Invalidate();
        HistoricalParkRolloutGate refreshed = await cache.GetOrCreateAsync(
            "park-1",
            "scope-a",
            factory,
            CancellationToken.None);

        Assert.Same(first, second);
        Assert.NotSame(second, refreshed);
        Assert.Equal(2, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_WhenPublicScopeChanges_ShouldUseAnotherEntry()
    {
        using MemoryCache memoryCache = new(new MemoryCacheOptions());
        using InMemoryHistoricalParkRolloutGateCache cache = new(memoryCache);
        int factoryCalls = 0;
        Func<CancellationToken, Task<HistoricalParkRolloutGate>> factory = _ =>
        {
            factoryCalls++;
            return Task.FromResult(new HistoricalParkRolloutGate(
                0,
                0,
                0,
                Array.Empty<int>()));
        };

        await cache.GetOrCreateAsync(
            "park-1",
            "scope-a",
            factory,
            CancellationToken.None);
        await cache.GetOrCreateAsync(
            "park-1",
            "scope-b",
            factory,
            CancellationToken.None);

        Assert.Equal(2, factoryCalls);
    }
}
