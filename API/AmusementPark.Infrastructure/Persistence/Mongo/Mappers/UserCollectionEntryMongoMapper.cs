using System.Globalization;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class UserCollectionEntryMongoMapper
{
    public static UserCollectionEntryDocument ToDocument(this UserCollectionEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);
        return new UserCollectionEntryDocument
        {
            Id = entry.Id.Value,
            UserId = entry.UserId,
            TargetType = entry.TargetType,
            TargetId = entry.TargetId,
            Kind = entry.Kind,
            TargetStatus = entry.TargetStatus,
            PrivateNote = entry.PrivateNote,
            Priority = entry.Priority,
            PreferredStartsOn = entry.PreferredPeriod?.StartsOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            PreferredEndsOn = entry.PreferredPeriod?.EndsOn?.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            Version = entry.Version,
            CreatedAt = entry.CreatedAtUtc,
            UpdatedAt = entry.UpdatedAtUtc,
        };
    }

    public static UserCollectionEntry ToDomain(this UserCollectionEntryDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        DateOnly? startsOn = ParseDate(document.PreferredStartsOn);
        DateOnly? endsOn = ParseDate(document.PreferredEndsOn);
        DateRangePreference? preferredPeriod = startsOn.HasValue || endsOn.HasValue
            ? new DateRangePreference(startsOn, endsOn)
            : null;
        return UserCollectionEntry.Restore(
            UserCollectionEntryId.Parse(document.Id),
            document.UserId,
            document.TargetType,
            document.TargetId,
            document.Kind,
            document.TargetStatus,
            document.PrivateNote,
            document.Priority,
            preferredPeriod,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }

    private static DateOnly? ParseDate(string? value)
    {
        return DateOnly.TryParseExact(
            value,
            "yyyy-MM-dd",
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateOnly date)
                ? date
                : null;
    }
}
