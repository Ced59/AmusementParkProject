using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record DurableBackgroundJobExecutionContext(
    string JobId,
    int PayloadVersion,
    JsonElement Payload,
    long? RequestedRevision,
    int AttemptCount,
    string? CorrelationId);
