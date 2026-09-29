using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Services.LiveData;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Services.LiveData;

public sealed class InMemoryPublicLiveForecastComputationCacheTests
{
    [Fact]
    public async Task GetOrCreateAsync_ShouldCacheUnavailableComputation()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryPublicLiveForecastComputationCache cache =
            new InMemoryPublicLiveForecastComputationCache(memoryCache);
        int factoryCalls = 0;

        PublicLiveForecastComputation? first = await cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<PublicLiveForecastComputation?>(null);
            },
            CancellationToken.None);
        PublicLiveForecastComputation? second = await cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<PublicLiveForecastComputation?>(CreateComputation());
            },
            CancellationToken.None);

        Assert.Null(first);
        Assert.Null(second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldReuseAvailableComputation()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryPublicLiveForecastComputationCache cache =
            new InMemoryPublicLiveForecastComputationCache(memoryCache);
        PublicLiveForecastComputation expected = CreateComputation();
        int factoryCalls = 0;

        PublicLiveForecastComputation? first = await cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<PublicLiveForecastComputation?>(expected);
            },
            CancellationToken.None);
        PublicLiveForecastComputation? second = await cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            _ =>
            {
                factoryCalls++;
                return Task.FromResult<PublicLiveForecastComputation?>(null);
            },
            CancellationToken.None);

        Assert.Same(expected, first);
        Assert.Same(expected, second);
        Assert.Equal(1, factoryCalls);
    }

    [Fact]
    public async Task GetOrCreateAsync_ShouldCollapseConcurrentComputationForTheSameKey()
    {
        using MemoryCache memoryCache = new MemoryCache(new MemoryCacheOptions());
        using InMemoryPublicLiveForecastComputationCache cache =
            new InMemoryPublicLiveForecastComputationCache(memoryCache);
        PublicLiveForecastComputation expected = CreateComputation();
        TaskCompletionSource firstFactoryStarted = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFactory = new TaskCompletionSource(
            TaskCreationOptions.RunContinuationsAsynchronously);
        int factoryCalls = 0;
        async Task<PublicLiveForecastComputation?> Factory(CancellationToken _)
        {
            Interlocked.Increment(ref factoryCalls);
            firstFactoryStarted.TrySetResult();
            await releaseFactory.Task;
            return expected;
        }

        Task<PublicLiveForecastComputation?> first = cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            Factory,
            CancellationToken.None);
        await firstFactoryStarted.Task;
        Task<PublicLiveForecastComputation?> second = cache.GetOrCreateAsync(
            "forecast:item-1:window-1",
            Factory,
            CancellationToken.None);
        releaseFactory.SetResult();

        PublicLiveForecastComputation?[] results = await Task.WhenAll(first, second);

        Assert.All(results, result => Assert.Same(expected, result));
        Assert.Equal(1, factoryCalls);
    }

    private static PublicLiveForecastComputation CreateComputation()
    {
        DateTime nowUtc = new DateTime(2026, 9, 29, 12, 30, 0, DateTimeKind.Utc);
        return new PublicLiveForecastComputation(
            "Europe/Paris",
            new LiveWaitForecast(
                nowUtc.AddMinutes(30),
                nowUtc.AddMinutes(90),
                nowUtc,
                25d,
                15d,
                35d,
                12),
            LiveWaitForecastBacktestPolicy.StudyVersion,
            LiveWaitForecastBacktestPolicy.CandidateMethod,
            LiveWaitForecastBacktestPolicy.IntervalMethod,
            4d,
            82d,
            nowUtc.AddDays(-90),
            nowUtc,
            120);
    }
}
