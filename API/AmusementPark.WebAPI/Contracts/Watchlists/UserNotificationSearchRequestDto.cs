namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationSearchRequestDto
{
    public int Page { get; init; } = 1;

    public int Size { get; init; } = 20;

    public bool UnreadOnly { get; init; }

    public string? ParkId { get; init; }

    public string? EventType { get; init; }
}
