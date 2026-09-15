using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record WatchSubscriptionPreferenceInput(
    CollectionTargetType TargetType,
    string TargetId,
    IReadOnlyCollection<FactualEventType> EventTypes,
    NotificationFrequency Frequency,
    IReadOnlyCollection<NotificationChannel> Channels);
