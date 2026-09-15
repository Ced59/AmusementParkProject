using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Models;

public sealed record FactualChangeOutboxEntry(
    string Id,
    string EventId,
    FactualEventType Type,
    int DefinitionVersion,
    ChangeTarget Target,
    FactValue? PreviousValue,
    FactValue? NewValue,
    SourceReference Source,
    DataConfidence Confidence,
    DateTime OccurredAtUtc,
    string DeduplicationKey,
    long SourceRevision,
    DateTime RecordedAtUtc,
    DateTime? MaterializedAtUtc,
    long Version)
{
    public bool IsMaterialized => this.MaterializedAtUtc.HasValue;
}
