using AmusementPark.Core.Domain.Visits;
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
            DateSortKey = UserVisitMongoDefinitions.ToDateSortKey(
                visit.Date.Year,
                visit.Date.Month,
                visit.Date.Day),
            TimeZoneId = visit.TimeZoneId,
            ServiceDayConvention = visit.ServiceDayConvention,
            Status = visit.Status,
            Privacy = visit.Privacy,
            Title = visit.Title,
            PrivateNote = visit.PrivateNote,
            Version = visit.Version,
            CreatedAt = visit.CreatedAtUtc,
            UpdatedAt = visit.UpdatedAtUtc,
            CompletedAtUtc = visit.CompletedAtUtc,
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
            document.CompletedAtUtc);
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

    private static VisitDate ToDomain(this VisitDateDocument document)
    {
        return new VisitDate(
            document.Year,
            document.Month,
            document.Day,
            document.Precision,
            document.IsApproximate);
    }
}
