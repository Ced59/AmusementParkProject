namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationPageResult(
    IReadOnlyCollection<UserNotificationResult> Items,
    int Page,
    int PageSize,
    long TotalItems,
    long UnreadCount,
    int RetentionDays,
    IReadOnlyCollection<UserNotificationParkFilterResult> ParkFilters);
