using System.Globalization;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Passport.Services;

internal sealed class PassportExportReferenceMap
{
    private readonly IReadOnlyDictionary<string, string> visitReferences;
    private readonly IReadOnlyDictionary<string, string> occurrenceReferences;
    private readonly IReadOnlyDictionary<string, string> parkReferences;
    private readonly IReadOnlyDictionary<(string ParkId, string ParkItemId), string>
        parkItemReferences;
    private readonly IReadOnlyDictionary<string, string> publicationReferences;
    private readonly IReadOnlyDictionary<string, string> invitationReferences;
    private readonly IReadOnlyDictionary<string, string> comparisonReferences;
    private readonly IReadOnlyDictionary<string, string> collectionReferences;
    private readonly IReadOnlyDictionary<string, string> subscriptionReferences;
    private readonly IReadOnlyDictionary<string, string> notificationReferences;
    private readonly IReadOnlyDictionary<string, string> digestReferences;
    private readonly IReadOnlyDictionary<string, string> deliveryReferences;

    private PassportExportReferenceMap(
        IReadOnlyDictionary<string, string> visitReferences,
        IReadOnlyDictionary<string, string> occurrenceReferences,
        IReadOnlyDictionary<string, string> parkReferences,
        IReadOnlyDictionary<(string ParkId, string ParkItemId), string> parkItemReferences,
        IReadOnlyDictionary<string, string> publicationReferences,
        IReadOnlyDictionary<string, string> invitationReferences,
        IReadOnlyDictionary<string, string> comparisonReferences,
        IReadOnlyDictionary<string, string> collectionReferences,
        IReadOnlyDictionary<string, string> subscriptionReferences,
        IReadOnlyDictionary<string, string> notificationReferences,
        IReadOnlyDictionary<string, string> digestReferences,
        IReadOnlyDictionary<string, string> deliveryReferences)
    {
        this.visitReferences = visitReferences;
        this.occurrenceReferences = occurrenceReferences;
        this.parkReferences = parkReferences;
        this.parkItemReferences = parkItemReferences;
        this.publicationReferences = publicationReferences;
        this.invitationReferences = invitationReferences;
        this.comparisonReferences = comparisonReferences;
        this.collectionReferences = collectionReferences;
        this.subscriptionReferences = subscriptionReferences;
        this.notificationReferences = notificationReferences;
        this.digestReferences = digestReferences;
        this.deliveryReferences = deliveryReferences;
    }

    public static PassportExportReferenceMap Create(PassportExportWriteRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        Dictionary<string, string> visits = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Visit visit in request.Visits)
        {
            AddReference(visits, visit.Id.Value, "visit");
        }

        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(visits, occurrence.VisitId.Value, "visit");
        }

        Dictionary<string, string> occurrences =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(occurrences, occurrence.Id.Value, "occurrence");
        }

        Dictionary<string, string> parks = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (Visit visit in request.Visits)
        {
            AddReference(parks, visit.ParkId, "park");
        }

        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            AddReference(parks, occurrence.ParkId, "park");
        }

        Dictionary<(string ParkId, string ParkItemId), string> parkItems =
            new Dictionary<(string ParkId, string ParkItemId), string>();
        foreach (RideOccurrence occurrence in request.RideOccurrences)
        {
            (string ParkId, string ParkItemId) target =
                (occurrence.ParkId, occurrence.ParkItemId);
            if (!parkItems.ContainsKey(target))
            {
                AddReference(parkItems, target, "park-item");
            }
        }

        Dictionary<string, string> publications =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (SharePublication publication in request.ShareLifecycle.Publications)
        {
            AddReference(publications, publication.Id.Value, "publication");
        }

        Dictionary<string, string> invitations =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ProfileComparisonInvitation invitation in request.ShareLifecycle.Invitations)
        {
            AddReference(invitations, invitation.Id.Value, "invitation");
        }

        Dictionary<string, string> comparisons =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ProfileComparison comparison in request.ShareLifecycle.Comparisons)
        {
            AddReference(comparisons, comparison.Id.Value, "comparison");
        }

        Dictionary<string, string> collections =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (UserCollectionEntry entry in request.WatchlistLifecycle.CollectionEntries)
        {
            AddReference(collections, entry.Id.Value, "collection");
        }

        Dictionary<string, string> subscriptions =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (WatchSubscription subscription in request.WatchlistLifecycle.Subscriptions)
        {
            AddReference(subscriptions, subscription.Id.Value, "watch");
        }

        Dictionary<string, string> notifications =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (UserNotification notification in request.WatchlistLifecycle.Notifications)
        {
            AddReference(notifications, notification.Id.Value, "notification");
        }

        Dictionary<string, string> digests =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (NotificationDigest digest in request.WatchlistLifecycle.Digests)
        {
            AddReference(digests, digest.Id.Value, "digest");
        }

        Dictionary<string, string> deliveries =
            new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (NotificationDeliveryAttempt attempt in request.WatchlistLifecycle.DeliveryAttempts)
        {
            AddReference(deliveries, attempt.Id, "delivery");
        }

        return new PassportExportReferenceMap(
            visits,
            occurrences,
            parks,
            parkItems,
            publications,
            invitations,
            comparisons,
            collections,
            subscriptions,
            notifications,
            digests,
            deliveries);
    }

    public string Visit(VisitId visitId)
    {
        return this.visitReferences[visitId.Value];
    }

    public string Occurrence(RideOccurrenceId occurrenceId)
    {
        return this.occurrenceReferences[occurrenceId.Value];
    }

    public string Park(string parkId)
    {
        return this.parkReferences[parkId];
    }

    public string ParkItem(string parkId, string parkItemId)
    {
        return this.parkItemReferences[(parkId, parkItemId)];
    }

    public string? VisitOrDefault(string visitId)
    {
        return this.visitReferences.GetValueOrDefault(visitId);
    }

    public string Publication(SharePublicationId publicationId)
    {
        return this.publicationReferences[publicationId.Value];
    }

    public string? PublicationOrDefault(SharePublicationId? publicationId)
    {
        return publicationId.HasValue
            ? this.publicationReferences.GetValueOrDefault(publicationId.Value.Value)
            : null;
    }

    public string Invitation(ProfileComparisonInvitationId invitationId)
    {
        return this.invitationReferences[invitationId.Value];
    }

    public string? InvitationOrDefault(ProfileComparisonInvitationId invitationId)
    {
        return this.invitationReferences.GetValueOrDefault(invitationId.Value);
    }

    public string Comparison(ProfileComparisonId comparisonId)
    {
        return this.comparisonReferences[comparisonId.Value];
    }

    public string? ComparisonOrDefault(ProfileComparisonId? comparisonId)
    {
        return comparisonId.HasValue
            ? this.comparisonReferences.GetValueOrDefault(comparisonId.Value.Value)
            : null;
    }

    public string Collection(UserCollectionEntryId entryId)
    {
        return this.collectionReferences[entryId.Value];
    }

    public string? SubscriptionOrDefault(WatchSubscriptionId subscriptionId)
    {
        return this.subscriptionReferences.GetValueOrDefault(subscriptionId.Value);
    }

    public string Notification(UserNotificationId notificationId)
    {
        return this.notificationReferences[notificationId.Value];
    }

    public string Digest(NotificationDigestId digestId)
    {
        return this.digestReferences[digestId.Value];
    }

    public string? DigestOrDefault(NotificationDigestId digestId)
    {
        return this.digestReferences.GetValueOrDefault(digestId.Value);
    }

    public string Delivery(string attemptId)
    {
        return this.deliveryReferences[attemptId];
    }

    private static void AddReference(
        IDictionary<string, string> references,
        string internalId,
        string prefix)
    {
        if (references.ContainsKey(internalId))
        {
            return;
        }

        string index = (references.Count + 1).ToString("D4", CultureInfo.InvariantCulture);
        references.Add(internalId, $"{prefix}-{index}");
    }

    private static void AddReference(
        IDictionary<(string ParkId, string ParkItemId), string> references,
        (string ParkId, string ParkItemId) internalTarget,
        string prefix)
    {
        string index = (references.Count + 1).ToString("D4", CultureInfo.InvariantCulture);
        references.Add(internalTarget, $"{prefix}-{index}");
    }
}
