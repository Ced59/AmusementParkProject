namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationTargetDto
{
    public string Type { get; init; } = string.Empty;

    public string TargetId { get; init; } = string.Empty;

    public string? ParkId { get; init; }

    public string? Name { get; init; }

    public string? ParentParkName { get; init; }

    public string? MainImageId { get; init; }
}
