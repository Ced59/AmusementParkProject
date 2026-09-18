using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripItemPreferenceMongoMapper
{
    public static TripItemPreferenceDocument ToDocument(this TripItemPreference preference)
    {
        ArgumentNullException.ThrowIfNull(preference);
        return new TripItemPreferenceDocument
        {
            Id = preference.Id.Value,
            TripPlanId = preference.TripPlanId.Value,
            MemberId = preference.MemberId.Value,
            UserId = preference.UserId,
            ParkItemId = preference.ParkItemId,
            Level = preference.Level,
            Reason = preference.Reason,
            Version = preference.Version,
            DocumentState = TripChildDocumentState.Committed,
            CreatedAt = ToMongoPrecision(preference.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(preference.UpdatedAtUtc),
        };
    }

    public static TripItemPreference ToDomain(this TripItemPreferenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripItemPreference.Restore(
            TripItemPreferenceId.Parse(document.Id),
            TripPlanId.Parse(document.TripPlanId),
            TripMemberId.Parse(document.MemberId),
            document.UserId,
            document.ParkItemId,
            document.Level,
            document.Reason,
            document.Version,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc));
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
