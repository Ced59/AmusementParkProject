using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserCollectionEntryResult(
    string EntryId,
    CollectionTargetType TargetType,
    string TargetId,
    UserCollectionKind Kind,
    CollectionTargetStatus TargetStatus,
    string? TargetName,
    string? ParentParkId,
    string? ParentParkName,
    string? MainImageId,
    string? PrivateNote,
    int? Priority,
    DateOnly? PreferredStartsOn,
    DateOnly? PreferredEndsOn,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    long Version);
