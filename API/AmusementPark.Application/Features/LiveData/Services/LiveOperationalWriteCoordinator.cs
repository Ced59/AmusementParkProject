using System.Collections.Concurrent;
using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Services;

public sealed class LiveOperationalWriteCoordinator
{
    private readonly ConcurrentDictionary<string, SemaphoreSlim> boundaries =
        new ConcurrentDictionary<string, SemaphoreSlim>(StringComparer.Ordinal);

    public async Task<TResult> RunAsync<TResult>(
        LiveDataSourceId sourceId,
        string externalEntityId,
        Func<CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(externalEntityId);
        ArgumentNullException.ThrowIfNull(operation);
        string key = $"{sourceId.Value}:{externalEntityId.Trim()}";
        SemaphoreSlim boundary = this.boundaries.GetOrAdd(
            key,
            static _ => new SemaphoreSlim(1, 1));
        await boundary.WaitAsync(cancellationToken);
        try
        {
            return await operation(CancellationToken.None);
        }
        finally
        {
            boundary.Release();
        }
    }
}
