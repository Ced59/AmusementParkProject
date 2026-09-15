namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationParkFilterDto
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkName { get; init; } = string.Empty;
}
