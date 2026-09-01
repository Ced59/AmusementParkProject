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

    Task<LeaseUnknownBackgroundJobResult> TryLeaseNextUnknownKindAsync(
        LeaseUnknownBackgroundJobRequest request,
        CancellationToken cancellationToken);

    Task<bool> RenewLeaseAsync(
        DurableBackgroundJobLease lease,
        TimeSpan leaseDuration,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJobCompletionResult?> CompleteAsync(
        DurableBackgroundJobLease lease,
        long? processedRevision,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJobStateTransitionResult?> ScheduleRetryAsync(
        DurableBackgroundJobLease lease,
        long? attemptedRevision,
        TimeSpan delay,
        string errorCode,
        CancellationToken cancellationToken);

    Task<DurableBackgroundJobStateTransitionResult?> DeadLetterAsync(
        DurableBackgroundJobLease lease,
        long? attemptedRevision,
        string errorCode,
        CancellationToken cancellationToken);

    Task<bool> CancelAsync(string jobId, CancellationToken cancellationToken);

    Task<int> ReleaseExpiredLeasesAsync(int maximumCount, CancellationToken cancellationToken);

    Task<bool> HasDeadLetteredRevisionAsync(
        string kind,
        string naturalKey,
        long requestedRevision,
        CancellationToken cancellationToken);

    Task<IReadOnlyCollection<DurableBackgroundJobDiagnosticItem>> ListDiagnosticsAsync(
        DurableBackgroundJobDiagnosticQuery query,
        CancellationToken cancellationToken);
}
