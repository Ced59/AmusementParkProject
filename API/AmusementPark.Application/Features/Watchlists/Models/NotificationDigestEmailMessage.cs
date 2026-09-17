using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Models;

public sealed record NotificationDigestEmailMessage(
    string RecipientEmail,
    string Language,
    NotificationFrequency Frequency,
    DateTime PeriodStartUtc,
    DateTime PeriodEndUtc,
    IReadOnlyCollection<NotificationDigestEmailEntry> Entries,
    int ObservedNotificationCount,
    string UnsubscribeToken);
