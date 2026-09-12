using System.Text.Json;

namespace AmusementPark.Application.Features.BackgroundJobs.Models;

public sealed record CoalesceBackgroundJobRequest(
    string Kind,
    string NaturalKey,
    long RequestedRevision,
    int PayloadVersion,
    JsonElement Payload,
    int Priority = 0,
    TimeSpan? Delay = null,
    string? CorrelationId = null);
