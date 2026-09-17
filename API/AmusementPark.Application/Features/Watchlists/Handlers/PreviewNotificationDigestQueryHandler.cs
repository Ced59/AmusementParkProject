using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class PreviewNotificationDigestQueryHandler
    : IQueryHandler<PreviewNotificationDigestQuery, NotificationDigestPreviewResult?>
{
    private readonly INotificationDigestRepository digestRepository;
    private readonly IWatchSubscriptionRepository subscriptionRepository;

    public PreviewNotificationDigestQueryHandler(
        INotificationDigestRepository digestRepository,
        IWatchSubscriptionRepository subscriptionRepository)
    {
        this.digestRepository = digestRepository ?? throw new ArgumentNullException(nameof(digestRepository));
        this.subscriptionRepository = subscriptionRepository
            ?? throw new ArgumentNullException(nameof(subscriptionRepository));
    }

    public async Task<NotificationDigestPreviewResult?> HandleAsync(
        PreviewNotificationDigestQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        NotificationDigestId digestId = NotificationDigestId.ForGroup(
            query.UserId,
            NotificationChannel.Email,
            query.Frequency,
            query.PeriodStartUtc);
        NotificationDigest? digest = await this.digestRepository.GetAsync(digestId, cancellationToken);
        if (digest is null)
        {
            return null;
        }

        WatchSubscriptionId[] subscriptionIds = digest.Entries
            .Select(static entry => entry.SubscriptionId)
            .Distinct()
            .ToArray();
        IReadOnlyCollection<WatchSubscription> subscriptions =
            await this.subscriptionRepository.ListOwnedByIdsAsync(
                digest.UserId,
                subscriptionIds,
                cancellationToken);
        Dictionary<WatchSubscriptionId, WatchSubscription> subscriptionsById = subscriptions
            .ToDictionary(static subscription => subscription.Id);
        int eligibleCount = digest.Entries.Count(entry =>
            subscriptionsById.TryGetValue(entry.SubscriptionId, out WatchSubscription? subscription)
            && !subscription.IsPaused
            && subscription.Frequency == digest.Frequency
            && subscription.Channels.Contains(digest.Channel)
            && subscription.EventTypes.Contains(entry.EventType));
        return new NotificationDigestPreviewResult(
            digest.Channel,
            digest.Frequency,
            digest.PeriodStartUtc,
            digest.PeriodEndUtc,
            digest.ObservedNotificationCount,
            digest.Entries.Count,
            eligibleCount,
            digest.Entries.Count - eligibleCount,
            digest.ObservedNotificationCount - digest.Entries.Count,
            digest.UpdatedAtUtc);
    }
}
