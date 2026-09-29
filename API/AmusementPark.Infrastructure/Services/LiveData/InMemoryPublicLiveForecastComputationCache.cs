using AmusementPark.Application.Features.LiveData.Models;
using AmusementPark.Application.Features.LiveData.Ports;
using Microsoft.Extensions.Caching.Memory;

namespace AmusementPark.Infrastructure.Services.LiveData;

public sealed class InMemoryPublicLiveForecastComputationCache :
    IPublicLiveForecastComputationCache,
    IDisposable
{
    private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(15);
    private static readonly object UnavailableSentinel = new object();
    private const int RefreshLockCount = 16;

    private readonly IMemoryCache memoryCache;
    private readonly SemaphoreSlim[] refreshLocks = Enumerable.Range(0, RefreshLockCount)
        .Select(static _ => new SemaphoreSlim(1, 1))
        .ToArray();

    public InMemoryPublicLiveForecastComputationCache(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    public async Task<PublicLiveForecastComputation?> GetOrCreateAsync(
        string cacheKey,
        Func<CancellationToken, Task<PublicLiveForecastComputation?>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(cacheKey);
        ArgumentNullException.ThrowIfNull(factory);

        if (this.TryRead(cacheKey, out PublicLiveForecastComputation? cachedComputation))
        {
            return cachedComputation;
        }

        int refreshLockIndex = (StringComparer.Ordinal.GetHashCode(cacheKey) & int.MaxValue)
            % RefreshLockCount;
        SemaphoreSlim refreshLock = this.refreshLocks[refreshLockIndex];
        await refreshLock.WaitAsync(cancellationToken);
        try
        {
            if (this.TryRead(cacheKey, out cachedComputation))
            {
                return cachedComputation;
            }

            PublicLiveForecastComputation? computation = await factory(cancellationToken);
            this.memoryCache.Set(
                cacheKey,
                computation is null ? UnavailableSentinel : computation,
                CacheDuration);
            return computation;
        }
        finally
        {
            refreshLock.Release();
        }
    }

    public void Dispose()
    {
        foreach (SemaphoreSlim refreshLock in this.refreshLocks)
        {
            refreshLock.Dispose();
        }
    }

    private bool TryRead(
        string cacheKey,
        out PublicLiveForecastComputation? computation)
    {
        if (!this.memoryCache.TryGetValue(cacheKey, out object? cachedValue)
            || cachedValue is null)
        {
            computation = null;
            return false;
        }

        computation = ReferenceEquals(cachedValue, UnavailableSentinel)
            ? null
            : (PublicLiveForecastComputation)cachedValue;
        return true;
    }
}
