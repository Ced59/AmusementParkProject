using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

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
