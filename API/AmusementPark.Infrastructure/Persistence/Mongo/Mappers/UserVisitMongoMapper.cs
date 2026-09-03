using AmusementPark.Core.Domain.Visits;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class UserVisitMongoMapper
{
    public static UserVisitDocument ToDocument(this Visit visit)
    {
        ArgumentNullException.ThrowIfNull(visit);

        return new UserVisitDocument
        {
            Id = visit.Id.Value,
            UserId = visit.UserId,
            ParkId = visit.ParkId,
            Date = visit.Date.ToDocument(),
            DateSortKey = visit.Date.ChronologicalOrderValue,
            TimeZoneId = visit.TimeZoneId,
            ServiceDayConvention = visit.ServiceDayConvention,
            Status = visit.Status,
            Privacy = visit.Privacy,
            Title = visit.Title,
            PrivateNote = visit.PrivateNote,
            ParkAssessment = visit.ParkAssessment?.ToDocument(),
            Version = visit.Version,
            CreatedAt = ToMongoPrecision(visit.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(visit.UpdatedAtUtc),
            CompletedAtUtc = ToMongoPrecision(visit.CompletedAtUtc),
        };
    }

    public static UserVisitCreationSnapshotDocument CreateCreationSnapshot(
        this UserVisitDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        return new UserVisitCreationSnapshotDocument
        {
            ParkId = document.ParkId,
            Date = new VisitDateDocument
            {
                Year = document.Date.Year,
                Month = document.Date.Month,
                Day = document.Date.Day,
                Precision = document.Date.Precision,
                IsApproximate = document.Date.IsApproximate,
            },
            TimeZoneId = document.TimeZoneId,
            ServiceDayConvention = document.ServiceDayConvention,
            Status = document.Status,
            Privacy = document.Privacy,
            Title = document.Title,
            PrivateNote = document.PrivateNote,
            Version = document.Version,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            CompletedAtUtc = document.CompletedAtUtc,
        };
    }

    public static Visit ToDomain(this UserVisitDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(document.Date);

        return Visit.Restore(
            VisitId.Parse(document.Id),
            document.UserId,
            document.ParkId,
            document.Date.ToDomain(),
            document.TimeZoneId,
            document.ServiceDayConvention,
            document.Status,
            document.Privacy,
            document.Title,
            document.PrivateNote,
            document.Version,
            document.CreatedAt,
            document.UpdatedAt,
            document.CompletedAtUtc,
            document.ParkAssessment?.ToDomain());
    }

    public static Visit CreationSnapshotToDomain(this UserVisitDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        UserVisitCreationSnapshotDocument snapshot = document.CreationSnapshot
            ?? throw new InvalidOperationException(
                "The idempotent visit creation snapshot is missing.");

        return Visit.Restore(
            VisitId.Parse(document.Id),
            document.UserId,
            snapshot.ParkId,
            snapshot.Date.ToDomain(),
            snapshot.TimeZoneId,
            snapshot.ServiceDayConvention,
            snapshot.Status,
            snapshot.Privacy,
            snapshot.Title,
            snapshot.PrivateNote,
            snapshot.Version,
            snapshot.CreatedAtUtc,
            snapshot.UpdatedAtUtc,
            snapshot.CompletedAtUtc);
    }

    private static VisitDateDocument ToDocument(this VisitDate date)
    {
        return new VisitDateDocument
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision,
            IsApproximate = date.IsApproximate,
        };
    }

    private static UserVisitParkAssessmentDocument ToDocument(
        this VisitParkAssessment assessment)
    {
        return new UserVisitParkAssessmentDocument
        {
            ValueHalfSteps = assessment.Value.HalfSteps,
            PrivateComment = assessment.PrivateComment,
            Revision = assessment.Revision,
            CreatedAtUtc = ToMongoPrecision(assessment.CreatedAtUtc),
            UpdatedAtUtc = ToMongoPrecision(assessment.UpdatedAtUtc),
        };
    }

    private static VisitParkAssessment ToDomain(
        this UserVisitParkAssessmentDocument document)
    {
        return VisitParkAssessment.Restore(
            RatingValue.FromHalfSteps(document.ValueHalfSteps),
            document.PrivateComment,
            document.Revision,
            document.CreatedAtUtc,
            document.UpdatedAtUtc);
    }

    private static VisitDate ToDomain(this VisitDateDocument document)
    {
        return new VisitDate(
            document.Year,
            document.Month,
            document.Day,
            document.Precision,
            document.IsApproximate);
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
