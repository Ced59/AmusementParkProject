using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class WatchSubscriptionMongoMapper
{
    public static WatchSubscriptionDocument ToDocument(this WatchSubscription subscription)
    {
        ArgumentNullException.ThrowIfNull(subscription);
        return new WatchSubscriptionDocument
        {
            Id = subscription.Id.Value,
            UserId = subscription.UserId,
            TargetType = subscription.TargetType,
            TargetId = subscription.TargetId,
            EventTypes = subscription.EventTypes.OrderBy(static type => type).ToList(),
            Frequency = subscription.Frequency,
            Channels = subscription.Channels.OrderBy(static channel => channel).ToList(),
            IsPaused = subscription.IsPaused,
            CreatedAt = subscription.CreatedAtUtc,
            UpdatedAt = subscription.UpdatedAtUtc,
            Version = subscription.Version,
        };
    }

    public static WatchSubscription ToDomain(this WatchSubscriptionDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return WatchSubscription.Restore(
            WatchSubscriptionId.Parse(document.Id),
            document.UserId,
            document.TargetType,
            document.TargetId,
            document.EventTypes,
            document.Frequency,
            document.Channels,
            document.IsPaused,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }
}
