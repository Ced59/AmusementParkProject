using AmusementPark.Core.Domain.FactualEvents;

namespace AmusementPark.Application.Features.FactualEvents.Results;

public sealed record FactualChangeEventAdminResult(
    string EventId,
    FactualEventType Type,
    int DefinitionVersion,
    FactualChangeTargetAdminResult Target,
    FactualFactValueAdminResult? PreviousValue,
    FactualFactValueAdminResult? NewValue,
    FactualSourceReferenceAdminResult Source,
    DataConfidence Confidence,
    DateTime OccurredAtUtc,
    long Revision,
    FactualChangeStatus Status,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime? VerifiedAtUtc,
    DateTime? PublishedAtUtc,
    DateTime? TerminalAtUtc,
    string? SupersededByEventId,
    string? ReasonCode,
    long Version,
    bool CanBeDistributed);
