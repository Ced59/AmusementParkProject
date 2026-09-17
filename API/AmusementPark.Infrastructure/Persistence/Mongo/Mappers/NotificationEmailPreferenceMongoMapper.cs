using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class NotificationEmailPreferenceMongoMapper
{
    public static NotificationEmailPreferenceDocument ToDocument(
        this NotificationEmailPreference preference)
    {
        ArgumentNullException.ThrowIfNull(preference);
        return new NotificationEmailPreferenceDocument
        {
            Id = preference.UserId,
            UserId = preference.UserId,
            IsEnabled = preference.IsEnabled,
            ConsentTextVersion = preference.ConsentTextVersion,
            ConsentLocale = preference.ConsentLocale,
            ConsentGrantedAt = preference.ConsentGrantedAtUtc,
            RevokedAt = preference.RevokedAtUtc,
            CreatedAt = preference.CreatedAtUtc,
            UpdatedAt = preference.UpdatedAtUtc,
            Version = preference.Version,
        };
    }

    public static NotificationEmailPreference ToDomain(
        this NotificationEmailPreferenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return NotificationEmailPreference.Restore(
            document.UserId,
            document.IsEnabled,
            document.ConsentTextVersion,
            document.ConsentLocale,
            document.ConsentGrantedAt,
            document.RevokedAt,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }
}
