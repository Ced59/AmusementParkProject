namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed record NotificationDigestPreviewDto(
    string Channel,
    string Frequency,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int ObservedNotificationCount,
    int GroupedItemCount,
    int EligibleItemCount,
    int SuppressedItemCount,
    int MergedCorrectionCount,
    DateTime UpdatedAtUtc);
