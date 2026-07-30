using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using Microsoft.Extensions.Caching.Memory;

namespace AmusementPark.Infrastructure.Services.Ratings;

public sealed class InMemoryRatingRankSnapshotCache : IRatingRankSnapshotCache, IDisposable
{
    private static readonly TimeSpan SnapshotDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache memoryCache;
    private readonly SemaphoreSlim refreshLock = new SemaphoreSlim(1, 1);
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
            if (this.memoryCache.TryGetValue(
                    cacheKey,
                    out IReadOnlyDictionary<string, int>? cachedRanks)
                && cachedRanks is not null)
            {
                return cachedRanks;
            }

            await this.refreshLock.WaitAsync(cancellationToken);
            try
            {
                if (this.memoryCache.TryGetValue(
                        cacheKey,
                        out cachedRanks)
                    && cachedRanks is not null)
                {
                    return cachedRanks;
                }

                long currentGeneration = Volatile.Read(ref this.generation);
                IReadOnlyDictionary<string, int> ranks = await factory(cancellationToken);
                if (currentGeneration != Volatile.Read(ref this.generation))
                {
                    continue;
                }

                this.memoryCache.Set(cacheKey, ranks, SnapshotDuration);
                return ranks;
            }
            finally
            {
                this.refreshLock.Release();
            }
        }
    }

    public void Invalidate()
    {
        Interlocked.Increment(ref this.generation);
        this.memoryCache.Remove(BuildCacheKey(RatingTargetType.Park, null));
        foreach (ParkItemCategory category in Enum.GetValues<ParkItemCategory>())
        {
            this.memoryCache.Remove(BuildCacheKey(RatingTargetType.ParkItem, category));
        }
    }

    public void Dispose()
    {
        this.refreshLock.Dispose();
    }

    private static string BuildCacheKey(
        RatingTargetType targetType,
        ParkItemCategory? parkItemCategory)
    {
        return targetType == RatingTargetType.Park
            ? "ratings:rank-snapshot:parks"
            : $"ratings:rank-snapshot:park-items:{parkItemCategory}";
    }
}
