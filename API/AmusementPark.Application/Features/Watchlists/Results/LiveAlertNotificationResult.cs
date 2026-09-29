using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record LiveAlertNotificationResult(
    string NotificationId,
    string SubscriptionId,
    string TargetId,
    string ParkId,
    string? TargetName,
    string? ParkName,
    string? MainImageId,
    LiveAlertType Type,
    int? ThresholdMinutes,
    LiveOperationalStatus? PreviousStatus,
    LiveOperationalStatus CurrentStatus,
    int? PreviousWaitMinutes,
    int? CurrentWaitMinutes,
    string SourceId,
    string? SourceName,
    string? AttributionText,
    string? AttributionUrl,
    DateTime ObservedAtUtc,
    DateTime DeliveredAtUtc,
    long AgeSecondsAtDelivery,
    UserNotificationStatus Status,
    DateTime? ReadAtUtc,
    DateTime ExpiresAtUtc,
    long Version);
