using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeCaptureRequest(
    FactualEventType Type,
    ChangeTarget Target,
    FactValue? PreviousValue,
    FactValue? NewValue,
    SourceReference Source,
    DataConfidence Confidence,
    DateTime OccurredAtUtc,
    string DeduplicationKey,
    long SourceRevision,
    DateTime RecordedAtUtc);
