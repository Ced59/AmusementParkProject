using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;

namespace AmusementPark.Infrastructure.Services.BackgroundJobs;

internal sealed class DurableBackgroundJobClaimCoordinator
{
    private readonly SemaphoreSlim claimGate = new SemaphoreSlim(1, 1);
    private readonly object activeCountGate = new object();
    private readonly IReadOnlyDictionary<string, DurableBackgroundJobHandlerDefinition> definitions;
    private readonly Dictionary<string, int> activeCounts = new Dictionary<string, int>(StringComparer.Ordinal);
    private string? unknownKindScanAfterKind;

    public DurableBackgroundJobClaimCoordinator(
        IReadOnlyCollection<DurableBackgroundJobHandlerDefinition> definitions)
    {
        ArgumentNullException.ThrowIfNull(definitions);
        this.definitions = definitions.ToDictionary(
            static definition => definition.Kind,
            StringComparer.Ordinal);
    }

    public async Task<DurableBackgroundJobClaim?> TryClaimAsync(
        IDurableBackgroundJobRepository repository,
        DurableBackgroundJobWorkload workload,
        string leaseOwner,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        await this.claimGate.WaitAsync(cancellationToken);
        try
        {
            string[] availableKinds;
            lock (this.activeCountGate)
            {
                availableKinds = this.definitions.Values
                    .Where(definition => definition.Workload == workload)
                    .Where(definition => this.GetActiveCount(definition.Kind) < definition.MaximumConcurrency)
                    .Select(static definition => definition.Kind)
                    .OrderBy(static kind => kind, StringComparer.Ordinal)
                    .ToArray();
            }

            if (availableKinds.Length == 0)
            {
                return null;
            }

            DurableBackgroundJob? job = await repository.TryLeaseNextAsync(
                new LeaseBackgroundJobRequest(availableKinds, leaseOwner, leaseDuration),
                cancellationToken);
            if (job is null)
            {
                return null;
            }

            lock (this.activeCountGate)
            {
                this.activeCounts[job.Kind] = this.GetActiveCount(job.Kind) + 1;
            }

            return new DurableBackgroundJobClaim(job, () => this.Release(job.Kind));
        }
        finally
        {
            this.claimGate.Release();
        }
    }

    public async Task<DurableBackgroundJobClaim?> TryClaimUnknownKindAsync(
        IDurableBackgroundJobRepository repository,
        string leaseOwner,
        TimeSpan leaseDuration,
        TimeSpan minimumAge,
        int maximumCandidateDocuments,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(repository);
        await this.claimGate.WaitAsync(cancellationToken);
        try
        {
            string[] knownKinds = this.definitions.Keys
                .OrderBy(static kind => kind, StringComparer.Ordinal)
                .ToArray();
            LeaseUnknownBackgroundJobResult result = await repository.TryLeaseNextUnknownKindAsync(
                new LeaseUnknownBackgroundJobRequest(
                    knownKinds,
                    leaseOwner,
                    leaseDuration,
                    minimumAge,
                    maximumCandidateDocuments,
                    this.unknownKindScanAfterKind),
                cancellationToken);
            this.unknownKindScanAfterKind = result.NextAfterKind;
            return result.Job is null
                ? null
                : new DurableBackgroundJobClaim(result.Job, static () => { });
        }
        finally
        {
            this.claimGate.Release();
        }
    }

    private int GetActiveCount(string kind)
    {
        return this.activeCounts.TryGetValue(kind, out int activeCount) ? activeCount : 0;
    }

    private void Release(string kind)
    {
        lock (this.activeCountGate)
        {
            int activeCount = this.GetActiveCount(kind);
            if (activeCount <= 1)
            {
                this.activeCounts.Remove(kind);
            }
            else
            {
                this.activeCounts[kind] = activeCount - 1;
            }
        }
    }
}

internal sealed class DurableBackgroundJobClaim : IDisposable
{
    private Action? release;

    public DurableBackgroundJobClaim(DurableBackgroundJob job, Action release)
    {
        ArgumentNullException.ThrowIfNull(job);
        ArgumentNullException.ThrowIfNull(release);
        this.Job = job;
        this.release = release;
    }

    public DurableBackgroundJob Job { get; }

    public async Task ReleaseAfterAsync(Task completion, IDisposable dependencyScope)
    {
        ArgumentNullException.ThrowIfNull(completion);
        ArgumentNullException.ThrowIfNull(dependencyScope);
        try
        {
            await completion;
        }
        finally
        {
            this.Dispose();
            dependencyScope.Dispose();
        }
    }

    public void Dispose()
    {
        Action? releaseAction = Interlocked.Exchange(ref this.release, null);
        releaseAction?.Invoke();
    }
}
