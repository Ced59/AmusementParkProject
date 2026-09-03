using System.Globalization;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class UserRideOccurrenceMongoMapper
{
    public const int CurrentSchemaVersion = 1;

    public static UserRideOccurrenceDocument ToDocument(this RideOccurrence occurrence)
    {
        ArgumentNullException.ThrowIfNull(occurrence);

        return new UserRideOccurrenceDocument
        {
            Id = occurrence.Id.Value,
            SchemaVersion = CurrentSchemaVersion,
            VisitId = occurrence.VisitId.Value,
            UserId = occurrence.UserId,
            ParkId = occurrence.ParkId,
            ParkItemId = occurrence.ParkItemId,
            SortPosition = occurrence.SortPosition,
            Moment = occurrence.Moment.ToDocument(),
            Status = occurrence.Status,
            Source = occurrence.Source,
            HistoricalConsistency = occurrence.HistoricalConsistency,
            HistoricalTarget = occurrence.HistoricalTarget.ToDocument(),
            PrivateNote = occurrence.PrivateNote,
            Version = occurrence.Version,
            CreatedAt = ToMongoPrecision(occurrence.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(occurrence.UpdatedAtUtc),
            DeletedAtUtc = ToMongoPrecision(occurrence.DeletedAtUtc),
        };
    }

    public static UserRideOccurrenceCreationSnapshotDocument CreateCreationSnapshot(
        this UserRideOccurrenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new UserRideOccurrenceCreationSnapshotDocument
        {
            VisitId = document.VisitId,
            ParkId = document.ParkId,
            ParkItemId = document.ParkItemId,
            SortPosition = document.SortPosition,
            Moment = document.Moment.Clone(),
            Status = document.Status,
            Source = document.Source,
            HistoricalConsistency = document.HistoricalConsistency,
            HistoricalTarget = document.HistoricalTarget.Clone(),
            PrivateNote = document.PrivateNote,
            Version = document.Version,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }

    public static RideOccurrence ToDomain(this UserRideOccurrenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Moment);

        return RideOccurrence.Restore(
            RideOccurrenceId.Parse(document.Id),
            VisitId.Parse(document.VisitId),
            document.UserId,
            document.ParkId,
            document.ParkItemId,
            document.SortPosition,
            document.Moment.ToDomain(),
            document.Status,
            document.Source,
            document.HistoricalConsistency,
            document.HistoricalTarget.ToDomain(),
            document.PrivateNote,
            document.Version,
            document.CreatedAt,
            document.UpdatedAt,
            document.DeletedAtUtc);
    }

    public static RideOccurrence CreationSnapshotToDomain(
        this UserRideOccurrenceDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        UserRideOccurrenceCreationSnapshotDocument snapshot = document.CreationSnapshot
            ?? throw new InvalidOperationException(
                "The idempotent ride occurrence creation snapshot is missing.");

        return RideOccurrence.Restore(
            RideOccurrenceId.Parse(document.Id),
            VisitId.Parse(snapshot.VisitId),
            document.UserId,
            snapshot.ParkId,
            snapshot.ParkItemId,
            snapshot.SortPosition,
            snapshot.Moment.ToDomain(),
            snapshot.Status,
            snapshot.Source,
            snapshot.HistoricalConsistency,
            snapshot.HistoricalTarget.ToDomain(),
            snapshot.PrivateNote,
            snapshot.Version,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            null);
    }

    private static RideOccurrenceMomentDocument ToDocument(this OccurrenceMoment moment)
    {
        return new RideOccurrenceMomentDocument
        {
            LocalTime = moment.LocalTime?.ToString("O", CultureInfo.InvariantCulture),
            IsApproximate = moment.IsApproximate,
        };
    }

    private static OccurrenceMoment ToDomain(this RideOccurrenceMomentDocument document)
    {
        TimeOnly? localTime = string.IsNullOrWhiteSpace(document.LocalTime)
            ? null
            : TimeOnly.ParseExact(
                document.LocalTime,
                "O",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None);
        return new OccurrenceMoment(localTime, document.IsApproximate);
    }

    private static HistoricalTargetReferenceDocument? ToDocument(
        this HistoricalTargetReference? reference)
    {
        return reference is null
            ? null
            : new HistoricalTargetReferenceDocument
            {
                Name = reference.Name,
                Category = reference.Category,
            };
    }

    private static HistoricalTargetReference? ToDomain(
        this HistoricalTargetReferenceDocument? document)
    {
        return document is null
            ? null
            : new HistoricalTargetReference(document.Name, document.Category);
    }

    private static RideOccurrenceMomentDocument Clone(this RideOccurrenceMomentDocument document)
    {
        return new RideOccurrenceMomentDocument
        {
            LocalTime = document.LocalTime,
            IsApproximate = document.IsApproximate,
        };
    }

    private static HistoricalTargetReferenceDocument? Clone(
        this HistoricalTargetReferenceDocument? document)
    {
        return document is null
            ? null
            : new HistoricalTargetReferenceDocument
            {
                Name = document.Name,
                Category = document.Category,
            };
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }

    private static DateTime? ToMongoPrecision(DateTime? value)
    {
        return value.HasValue ? ToMongoPrecision(value.Value) : null;
    }
}
