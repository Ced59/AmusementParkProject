using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

internal sealed record UserCollectionTargetSnapshot(
    CollectionTargetType TargetType,
    string TargetId,
    CollectionTargetStatus Status,
    string? Name,
    string? ParentParkId,
    string? ParentParkName,
    string? MainImageId)
{
    public bool IsAvailableForCreation => !string.IsNullOrWhiteSpace(this.Name);
}
