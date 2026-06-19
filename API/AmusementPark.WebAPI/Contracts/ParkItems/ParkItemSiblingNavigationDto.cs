namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemSiblingNavigationDto
{
    public string ParkId { get; init; } = string.Empty;

    public string CurrentItemId { get; init; } = string.Empty;

    public int CurrentPosition { get; init; }

    public int TotalItems { get; init; }

    public int RemainingItems { get; init; }

    public ParkItemSiblingNavigationItemDto? Previous { get; init; }

    public ParkItemSiblingNavigationItemDto? Next { get; init; }
}

public sealed class ParkItemSiblingNavigationItemDto
{
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
