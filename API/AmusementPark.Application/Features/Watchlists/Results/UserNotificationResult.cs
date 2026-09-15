using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record UserNotificationResult(
    string NotificationId,
    FactualEventType EventType,
    UserNotificationStatus Status,
    UserNotificationTargetResult Target,
    UserNotificationFactValueResult? PreviousValue,
    UserNotificationFactValueResult? NewValue,
    UserNotificationSourceResult Source,
    DateTime OccurredAtUtc,
    DateTime DeliveredAtUtc,
    DateTime? ReadAtUtc,
    DateTime ExpiresAtUtc,
    long Version,
    bool CanManageSubscription,
    long? SubscriptionVersion);
