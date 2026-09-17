using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record NotificationDigestPreviewResult(
    NotificationChannel Channel,
    NotificationFrequency Frequency,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    int ObservedNotificationCount,
    int GroupedItemCount,
    int EligibleItemCount,
    int SuppressedItemCount,
    int MergedCorrectionCount,
    DateTime UpdatedAtUtc);
