using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Results;

public sealed record WatchSubscriptionResult(
    string SubscriptionId,
    CollectionTargetType TargetType,
    string TargetId,
    string? TargetName,
    string? ParentParkId,
    string? ParentParkName,
    string? MainImageId,
    IReadOnlyCollection<FactualEventType> EventTypes,
    NotificationFrequency Frequency,
    IReadOnlyCollection<NotificationChannel> Channels,
    bool IsPaused,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    long Version);
