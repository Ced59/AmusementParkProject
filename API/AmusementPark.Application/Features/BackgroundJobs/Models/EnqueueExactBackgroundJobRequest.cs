using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record EnqueueExactBackgroundJobRequest(
    string Kind,
    string IdempotencyKey,
    int PayloadVersion,
    JsonElement Payload,
    int Priority = 0,
    TimeSpan? Delay = null,
    string? CorrelationId = null);
