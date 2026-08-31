using AmusementPark.Application.Features.BackgroundJobs.Models;

namespace AmusementPark.Application.Features.BackgroundJobs.Ports;

public interface IDurableBackgroundJobRepository
{
    Task<DurableBackgroundJob> EnqueueExactAsync(
        EnqueueExactBackgroundJobRequest request,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJob> CoalesceAsync(
        CoalesceBackgroundJobRequest request,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJob?> TryLeaseNextAsync(
        LeaseBackgroundJobRequest request,
        CancellationToken cancellationToken);

    Task<bool> RenewLeaseAsync(
        DurableBackgroundJobLease lease,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJobCompletionResult?> CompleteAsync(
        DurableBackgroundJobLease lease,
        long? processedRevision,
        CancellationToken cancellationToken);

    Task<bool> ScheduleRetryAsync(
        DurableBackgroundJobLease lease,
        TimeSpan delay,
        string errorCode,
        CancellationToken cancellationToken);

    Task<bool> DeadLetterAsync(
        DurableBackgroundJobLease lease,
        string errorCode,
        CancellationToken cancellationToken);

    Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken);

    Task<int> ReleaseExpiredLeasesAsync(int maximumCount, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DurableBackgroundJobDiagnosticItem>> ListDiagnosticsAsync(
        DurableBackgroundJobDiagnosticQuery query,
        CancellationToken cancellationToken);
}
