using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Application.Features.Watchlists.Models;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationResult(
    string NotificationId,
    FactualEventType EventType,
    UserNotificationStatus Status,
    UserNotificationNoticeKind NoticeKind,
    FactualChangeStatus FactualStatus,
    DateTime? LifecycleAtUtc,
    string? RetractionReasonCode,
    UserNotificationTargetResult Target,
    UserNotificationFactValueResult? PreviousValue,
    UserNotificationFactValueResult? NewValue,
    UserNotificationSourceResult Source,
    DateTime OccurredAtUtc,
    DateTime DeliveredAtUtc,
    DateTime? ReadAtUtc,
    DateTime ExpiresAtUtc,
    long Version,
    bool IsReportedMisleading,
    bool CanManageSubscription,
    long? SubscriptionVersion);
