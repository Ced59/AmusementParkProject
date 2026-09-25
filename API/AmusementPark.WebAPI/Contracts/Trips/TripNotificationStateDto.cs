namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripNotificationStateDto(
    bool Enabled,
    long Version,
    int UnreadCount,
    bool HasMoreUnread);
