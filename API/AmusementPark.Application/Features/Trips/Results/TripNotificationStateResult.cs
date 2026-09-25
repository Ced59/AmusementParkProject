namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripNotificationStateResult(
    bool Enabled,
    long Version,
    int UnreadCount,
    bool HasMoreUnread);
