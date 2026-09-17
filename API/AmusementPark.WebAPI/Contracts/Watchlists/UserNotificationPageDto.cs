namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class UserNotificationPageDto
{
    public IReadOnlyCollection<UserNotificationDto> Items { get; init; } = Array.Empty<UserNotificationDto>();

    public int Page { get; init; }

    public int PageSize { get; init; }

    public long TotalItems { get; init; }

    public int TotalPages { get; init; }

    public long UnreadCount { get; init; }

    public int RetentionDays { get; init; }

    public IReadOnlyCollection<UserNotificationParkFilterDto> ParkFilters { get; init; } =
        Array.Empty<UserNotificationParkFilterDto>();
}
