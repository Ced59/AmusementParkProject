using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

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
