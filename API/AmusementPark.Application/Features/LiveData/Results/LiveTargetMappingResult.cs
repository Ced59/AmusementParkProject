using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveTargetMappingResult(
    Guid MappingId,
    string Version,
    string SourceId,
    LiveExternalTargetResult ExternalTarget,
    LiveInternalTargetResult? Target,
    LiveMappingStatus Status,
    LiveMappingConfidence Confidence,
    DateTime ValidFromUtc,
    DateTime? ValidToUtc,
    int Revision,
    int? SupersedesRevision,
    string? ReviewedByUserId,
    string? ReviewNote,
    DateTime RecordedAtUtc,
    bool IsEligibleForLiveUse);
