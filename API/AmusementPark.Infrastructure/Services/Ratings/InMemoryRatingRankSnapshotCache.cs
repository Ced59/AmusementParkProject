using System.Collections.Concurrent;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Caching.Memory;

namespace AmusementPark.Infrastructure.Services.Ratings;

public sealed class InMemoryRatingRankSnapshotCache : IRatingRankSnapshotCache, IDisposable
{
    private static readonly TimeSpan SnapshotDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache memoryCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> refreshLocks =
        new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);
    private long generation;

    public InMemoryRatingRankSnapshotCache(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetOrCreateAsync(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory,
        Func<CancellationToken, Task<IReadOnlyDictionary<string, int>>> factory,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factory);

        string cacheKey = BuildCacheKey(targetType, parkItemCategory);
        while (true)
        {
            long currentGeneration = Volatile.Read(ref this.generation);
            if (this.memoryCache.TryGetValue(
                    cacheKey,
                    out RatingRankSnapshot? cachedSnapshot)
                && cachedSnapshot is not null
                && cachedSnapshot.Generation == currentGeneration)
            {
                return cachedSnapshot.Ranks;
            }

            SemaphoreSlim refreshLock = this.refreshLocks.GetOrAdd(
                cacheKey,
                static _ => new SemaphoreSlim(1, 1));
            await refreshLock.WaitAsync(cancellationToken);
            try
            {
                currentGeneration = Volatile.Read(ref this.generation);
                if (this.memoryCache.TryGetValue(
                        cacheKey,
                        out cachedSnapshot)
                    && cachedSnapshot is not null
                    && cachedSnapshot.Generation == currentGeneration)
                {
                    return cachedSnapshot.Ranks;
                }

                IReadOnlyDictionary<string, int> ranks = await factory(cancellationToken);
                if (currentGeneration != Volatile.Read(ref this.generation))
                {
                    continue;
                }

                this.memoryCache.Set(
                    cacheKey,
                    new RatingRankSnapshot(currentGeneration, ranks),
                    SnapshotDuration);
                return ranks;
            }
            finally
            {
                refreshLock.Release();
            }
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref this.generation);
    }

    public void Dispose()
    {
        foreach (SemaphoreSlim refreshLock in this.refreshLocks.Values)
        {
            refreshLock.Dispose();
        }
    }

    private static string BuildCacheKey(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory)
    {
        return targetType == RatingTargetType.Park
            ? "ratings:rank-snapshot:parks"
            : $"ratings:rank-snapshot:park-items:{parkItemCategory}";
    }

    private sealed record RatingRankSnapshot(
        long Generation,
        IReadOnlyDictionary<string, int> Ranks);
}
