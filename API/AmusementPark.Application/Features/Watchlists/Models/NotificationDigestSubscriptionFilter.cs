using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record NotificationDigestSubscriptionFilter(
    WatchSubscriptionId SubscriptionId,
    IReadOnlyCollection<FactualEventType> EventTypes);
