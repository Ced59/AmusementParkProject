using AmusementPark.Application.Features.ParkItems.Contracts;

namespace AmusementPark.Application.Features.ParkItems.Results;

public sealed class ParkItemSiblingNavigationResult
{
    public string ParkId { get; init; } = string.Empty;

    public string CurrentItemId { get; init; } = string.Empty;

    public int CurrentPosition { get; init; }

    public int TotalItems { get; init; }

    public int RemainingItems { get; init; }

    public ParkItemSiblingNavigationItem? Previous { get; init; }

    public ParkItemSiblingNavigationItem? Next { get; init; }
}
