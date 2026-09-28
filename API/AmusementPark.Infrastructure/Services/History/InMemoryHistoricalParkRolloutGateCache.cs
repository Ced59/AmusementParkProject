using System.Collections.Concurrent;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using Microsoft.Extensions.Caching.Memory;

namespace AmusementPark.Infrastructure.Services.History;

public sealed class InMemoryHistoricalParkRolloutGateCache :
    IHistoricalParkRolloutGateCache,
    IDisposable
{
    private static readonly TimeSpan GateDuration = TimeSpan.FromMinutes(5);

    private readonly IMemoryCache memoryCache;
    private readonly ConcurrentDictionary<string, SemaphoreSlim> refreshLocks =
        new(StringComparer.Ordinal);
    private long generation;

    public InMemoryHistoricalParkRolloutGateCache(IMemoryCache memoryCache)
    {
        this.memoryCache = memoryCache;
    }

    public async Task<HistoricalParkRolloutGate> GetOrCreateAsync(
        string parkId,
        string scopeFingerprint,
        Func<CancellationToken, Task<HistoricalParkRolloutGate>> factory,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        if (string.IsNullOrWhiteSpace(scopeFingerprint))
        {
            throw new ArgumentException("A public scope fingerprint is required.", nameof(scopeFingerprint));
        }

        ArgumentNullException.ThrowIfNull(factory);
        string cacheKey = $"history:park-rollout-gate:{parkId.Trim()}:{scopeFingerprint.Trim()}";
        string refreshLockKey = $"history:park-rollout-gate-lock:{parkId.Trim()}";
        while (true)
        {
            long currentGeneration = Volatile.Read(ref this.generation);
            if (this.TryGetCurrent(cacheKey, currentGeneration, out HistoricalParkRolloutGate cachedGate))
            {
                return cachedGate;
            }

            SemaphoreSlim refreshLock = this.refreshLocks.GetOrAdd(
                refreshLockKey,
                static _ => new SemaphoreSlim(1, 1));
            await refreshLock.WaitAsync(cancellationToken);
            try
            {
                currentGeneration = Volatile.Read(ref this.generation);
                if (this.TryGetCurrent(cacheKey, currentGeneration, out cachedGate))
                {
                    return cachedGate;
                }

                HistoricalParkRolloutGate gate = await factory(cancellationToken);
                if (currentGeneration != Volatile.Read(ref this.generation))
                {
                    continue;
                }

                this.memoryCache.Set(cacheKey, (currentGeneration, gate), GateDuration);
                return gate;
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

    private bool TryGetCurrent(
        string cacheKey,
        long currentGeneration,
        out HistoricalParkRolloutGate gate)
    {
        if (this.memoryCache.TryGetValue(cacheKey, out object? cachedValue)
            && cachedValue is ValueTuple<long, HistoricalParkRolloutGate> cachedGate
            && cachedGate.Item1 == currentGeneration)
        {
            gate = cachedGate.Item2;
            return true;
        }

        gate = null!;
        return false;
    }
}
