using System.Globalization;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class TripDayPlanMongoMapper
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static TripDayPlanDocument ToDocument(this TripDayPlan dayPlan)
    {
        ArgumentNullException.ThrowIfNull(dayPlan);
        return new TripDayPlanDocument
        {
            Id = dayPlan.Id.Value,
            TripPlanId = dayPlan.TripPlanId.Value,
            LocalDate = dayPlan.LocalDate.ToString(DateFormat, CultureInfo.InvariantCulture),
            ParkCandidateId = dayPlan.ParkCandidateId.Value,
            ParkId = dayPlan.ParkId,
            DesiredArrivalTime = dayPlan.DesiredArrivalTime?.ToString(TimeFormat, CultureInfo.InvariantCulture),
            GroupNote = dayPlan.GroupNote,
            Blocks = dayPlan.Blocks.Select(static block => block.ToDocument()).ToList(),
            Version = dayPlan.Version,
            CreatedAt = ToMongoPrecision(dayPlan.CreatedAtUtc),
            UpdatedAt = ToMongoPrecision(dayPlan.UpdatedAtUtc),
        };
    }

    public static TripDayPlan ToDomain(this TripDayPlanDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return TripDayPlan.Restore(
            TripDayPlanId.Parse(document.Id),
            TripPlanId.Parse(document.TripPlanId),
            DateOnly.ParseExact(document.LocalDate, DateFormat, CultureInfo.InvariantCulture),
            TripParkCandidateId.Parse(document.ParkCandidateId),
            document.ParkId,
            ParseTime(document.DesiredArrivalTime),
            document.GroupNote,
            document.Blocks.Select(static block => block.ToDomain()).ToArray(),
            document.Version,
            DateTime.SpecifyKind(document.CreatedAt, DateTimeKind.Utc),
            DateTime.SpecifyKind(document.UpdatedAt, DateTimeKind.Utc));
    }

    private static TripDayBlockDocument ToDocument(this TripDayBlock block)
    {
        return new TripDayBlockDocument
        {
            BlockId = block.Id.Value,
            Type = block.Type,
            Title = block.Title,
            Details = block.Details,
            LocalTime = block.LocalTime?.ToString(TimeFormat, CultureInfo.InvariantCulture),
            SortPosition = block.SortPosition,
        };
    }

    private static TripDayBlock ToDomain(this TripDayBlockDocument document)
    {
        return new TripDayBlock(
            TripDayBlockId.Parse(document.BlockId),
            document.Type,
            document.Title,
            document.Details,
            ParseTime(document.LocalTime),
            document.SortPosition);
    }

    private static TimeOnly? ParseTime(string? value)
    {
        return value is null
            ? null
            : TimeOnly.ParseExact(value, TimeFormat, CultureInfo.InvariantCulture);
    }

    private static DateTime ToMongoPrecision(DateTime value)
    {
        long ticks = value.Ticks - (value.Ticks % TimeSpan.TicksPerMillisecond);
        return new DateTime(ticks, DateTimeKind.Utc);
    }
}
