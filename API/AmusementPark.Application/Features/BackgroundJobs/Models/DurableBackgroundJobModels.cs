using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public enum DurableBackgroundJobStatus
{
    Pending,
    Leased,
    Succeeded,
    RetryScheduled,
    DeadLetter,
    Cancelled,
    Superseded,
}

public sealed record DurableBackgroundJob(
    string Id,
    string Kind,
    string? NaturalKey,
    string? IdempotencyKey,
    int PayloadVersion,
    JsonElement Payload,
    long? RequestedRevision,
    long? ProcessedRevision,
    DurableBackgroundJobStatus Status,
    int Priority,
    int AttemptCount,
    DateTime NotBeforeUtc,
    string? LeaseOwner,
    string? LeaseToken,
    DateTime? LeaseExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    string? LastErrorCode,
    string? CorrelationId);

public sealed record EnqueueExactBackgroundJobRequest(
    string Kind,
    string IdempotencyKey,
    int PayloadVersion,
    JsonElement Payload,
    int Priority = 0,
    TimeSpan? Delay = null,
    string? CorrelationId = null);

public sealed record CoalesceBackgroundJobRequest(
    string Kind,
    string NaturalKey,
    long RequestedRevision,
    int PayloadVersion,
    JsonElement Payload,
    int Priority = 0,
    TimeSpan? Delay = null,
    string? CorrelationId = null);

public sealed record LeaseBackgroundJobRequest(
    IReadOnlyCollection<string> Kinds,
    string LeaseOwner,
    TimeSpan LeaseDuration);

public sealed record LeaseUnknownBackgroundJobRequest(
    IReadOnlyCollection<string> KnownKinds,
    string LeaseOwner,
    TimeSpan LeaseDuration,
    TimeSpan MinimumAge,
    int MaximumCandidateDocuments,
    string? AfterKind = null);

public sealed record LeaseUnknownBackgroundJobResult(
    DurableBackgroundJob? Job,
    string? NextAfterKind);

public sealed record DurableBackgroundJobLease(
    string JobId,
    string LeaseOwner,
    string LeaseToken);

public sealed record DurableBackgroundJobCompletionResult(
    string JobId,
    DurableBackgroundJobStatus Status,
    long? RequestedRevision,
    long? ProcessedRevision);

public sealed record DurableBackgroundJobStateTransitionResult(
    string JobId,
    DurableBackgroundJobStatus Status,
    long? RequestedRevision,
    long? ProcessedRevision);

public sealed record DurableBackgroundJobDiagnosticQuery(
    IReadOnlyCollection<DurableBackgroundJobStatus>? Statuses = null,
    string? Kind = null,
    int Limit = 100);

public sealed record DurableBackgroundJobDiagnosticItem(
    string Id,
    string Kind,
    string? NaturalKey,
    DurableBackgroundJobStatus Status,
    int Priority,
    int AttemptCount,
    long? RequestedRevision,
    long? ProcessedRevision,
    DateTime NotBeforeUtc,
    DateTime? LeaseExpiresAtUtc,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? CompletedAtUtc,
    string? LastErrorCode,
    string? CorrelationId);
