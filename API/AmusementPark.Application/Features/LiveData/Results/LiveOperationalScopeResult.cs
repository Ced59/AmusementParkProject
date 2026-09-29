using AmusementPark.Core.Domain.LiveData;

namespace AmusementPark.Application.Features.LiveData.Results;

public sealed record LiveOperationalScopeResult(
    LiveOperationalScopeType ScopeType,
    string SourceId,
    string? ExternalEntityId,
    string? InternalParkId,
    LiveTargetType? TargetType,
    string? InternalTargetId,
    string DisplayName,
    string? ParentDisplayName,
    bool CollectionEnabled,
    bool PublicReadEnabled,
    bool EffectiveCollectionEnabled,
    bool EffectivePublicReadEnabled,
    int Revision,
    string? Reason,
    string? ChangedByUserId,
    DateTime? RecordedAtUtc);
