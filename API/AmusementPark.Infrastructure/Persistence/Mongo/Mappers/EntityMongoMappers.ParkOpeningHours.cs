using System.Globalization;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkOpeningHours;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static partial class EntityMongoMappers
{
    private const string OpeningHoursTimeFormat = "HH:mm";

    public static ParkOpeningHoursScheduleDocument ToDocument(this ParkOpeningHoursSchedule schedule)
    {
        DateOnly? firstDate = ResolveFirstOpeningHoursDate(schedule);
        DateOnly? lastDate = ResolveLastOpeningHoursDate(schedule);

        return new ParkOpeningHoursScheduleDocument
        {
            Id = string.IsNullOrWhiteSpace(schedule.Id) ? Guid.NewGuid().ToString("N") : schedule.Id,
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            FirstDate = firstDate.HasValue ? FormatDate(firstDate.Value) : null,
            LastDate = lastDate.HasValue ? FormatDate(lastDate.Value) : null,
            HasScheduleData = schedule.RegularRules.Count > 0 || schedule.DateOverrides.Count > 0,
            CoverageSegments = schedule.CoverageSegments.Select(static segment => segment.ToDocument()).ToList(),
            CreatedAt = schedule.CreatedAtUtc,
            UpdatedAt = schedule.UpdatedAtUtc,
            RegularRules = schedule.RegularRules.Select(static rule => rule.ToDocument()).ToList(),
            DateOverrides = schedule.DateOverrides.Select(static dateOverride => dateOverride.ToDocument()).ToList(),
        };
    }

    public static ParkOpeningHoursSchedule ToDomain(this ParkOpeningHoursScheduleDocument document)
    {
        return new ParkOpeningHoursSchedule
        {
            Id = document.Id,
            ParkId = document.ParkId,
            TimeZoneId = document.TimeZoneId,
            SourceUrl = document.SourceUrl,
            Notes = document.Notes,
            LastVerifiedAtUtc = document.LastVerifiedAtUtc,
            CreatedAtUtc = document.CreatedAt,
            UpdatedAtUtc = document.UpdatedAt,
            RegularRules = document.RegularRules.Select(static rule => rule.ToDomain()).ToList(),
            DateOverrides = document.DateOverrides.Select(static dateOverride => dateOverride.ToDomain()).ToList(),
            CoverageSegments = document.CoverageSegments.Select(static segment => segment.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursCoverageSegmentDocument ToDocument(this ParkOpeningHoursCoverageSegment segment)
    {
        return new ParkOpeningHoursCoverageSegmentDocument
        {
            StartDate = FormatDate(segment.StartDate),
            EndDate = FormatDate(segment.EndDate),
        };
    }

    private static ParkOpeningHoursCoverageSegment ToDomain(this ParkOpeningHoursCoverageSegmentDocument document)
    {
        return new ParkOpeningHoursCoverageSegment
        {
            StartDate = ParseDate(document.StartDate),
            EndDate = ParseDate(document.EndDate),
        };
    }

    private static ParkOpeningHoursRuleDocument ToDocument(this ParkOpeningHoursRule rule)
    {
        return new ParkOpeningHoursRuleDocument
        {
            Id = string.IsNullOrWhiteSpace(rule.Id) ? Guid.NewGuid().ToString("N") : rule.Id,
            StartDate = FormatDate(rule.StartDate),
            EndDate = FormatDate(rule.EndDate),
            DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(),
            IsClosed = rule.IsClosed,
            Label = rule.Label,
            Reason = rule.Reason,
            SortOrder = rule.SortOrder,
            TimeRanges = rule.TimeRanges.Select(static timeRange => timeRange.ToDocument()).ToList(),
        };
    }

    private static ParkOpeningHoursRule ToDomain(this ParkOpeningHoursRuleDocument document)
    {
        return new ParkOpeningHoursRule
        {
            Id = document.Id,
            StartDate = ParseDate(document.StartDate),
            EndDate = ParseDate(document.EndDate),
            DaysOfWeek = document.DaysOfWeek
                .Select(static value => Enum.TryParse(value, true, out DayOfWeek parsed) ? parsed : (DayOfWeek?)null)
                .Where(static value => value.HasValue)
                .Select(static value => value!.Value)
                .ToList(),
            IsClosed = document.IsClosed,
            Label = document.Label,
            Reason = document.Reason,
            SortOrder = document.SortOrder,
            TimeRanges = document.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverrideDocument ToDocument(this ParkOpeningHoursDateOverride dateOverride)
    {
        return new ParkOpeningHoursDateOverrideDocument
        {
            LocalDate = FormatDate(dateOverride.LocalDate),
            IsClosed = dateOverride.IsClosed,
            Label = dateOverride.Label,
            Reason = dateOverride.Reason,
            TimeRanges = dateOverride.TimeRanges.Select(static timeRange => timeRange.ToDocument()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverride ToDomain(this ParkOpeningHoursDateOverrideDocument document)
    {
        return new ParkOpeningHoursDateOverride
        {
            LocalDate = ParseDate(document.LocalDate),
            IsClosed = document.IsClosed,
            Label = document.Label,
            Reason = document.Reason,
            TimeRanges = document.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursTimeRangeDocument ToDocument(this ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkOpeningHoursTimeRangeDocument
        {
            OpensAt = FormatTime(timeRange.OpensAt),
            ClosesAt = FormatTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? FormatTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    private static ParkOpeningHoursTimeRange ToDomain(this ParkOpeningHoursTimeRangeDocument document)
    {
        return new ParkOpeningHoursTimeRange
        {
            OpensAt = ParseTime(document.OpensAt),
            ClosesAt = ParseTime(document.ClosesAt),
            ClosesNextDay = document.ClosesNextDay,
            LastAdmissionAt = string.IsNullOrWhiteSpace(document.LastAdmissionAt) ? null : ParseTime(document.LastAdmissionAt),
            LastAdmissionNextDay = document.LastAdmissionNextDay,
        };
    }

    private static string FormatTime(TimeOnly time)
    {
        return time.ToString(OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }

    private static TimeOnly ParseTime(string time)
    {
        return TimeOnly.ParseExact(time, OpeningHoursTimeFormat, CultureInfo.InvariantCulture);
    }

    private static DateOnly? ResolveFirstOpeningHoursDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.StartDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Min();
    }

    private static DateOnly? ResolveLastOpeningHoursDate(ParkOpeningHoursSchedule schedule)
    {
        List<DateOnly> dates = new List<DateOnly>();
        dates.AddRange(schedule.RegularRules.Select(static rule => rule.EndDate));
        dates.AddRange(schedule.DateOverrides.Select(static dateOverride => dateOverride.LocalDate));
        return dates.Count == 0 ? null : dates.Max();
    }
}
