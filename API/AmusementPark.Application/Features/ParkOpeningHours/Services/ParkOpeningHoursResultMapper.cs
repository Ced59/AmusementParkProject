using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

internal static class ParkOpeningHoursResultMapper
{
    public static ParkOpeningHoursScheduleResult ToScheduleResult(this ParkOpeningHoursSchedule schedule)
    {
        return new ParkOpeningHoursScheduleResult
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            CreatedAtUtc = schedule.CreatedAtUtc,
            UpdatedAtUtc = schedule.UpdatedAtUtc,
            RegularRules = schedule.RegularRules.Select(static rule => rule.ToResult()).ToList(),
            DateOverrides = schedule.DateOverrides.Select(static dateOverride => dateOverride.ToResult()).ToList(),
        };
    }

    public static ParkOpeningHoursRuleResult ToResult(this ParkOpeningHoursRule rule)
    {
        return new ParkOpeningHoursRuleResult
        {
            Id = rule.Id ?? string.Empty,
            StartDate = rule.StartDate,
            EndDate = rule.EndDate,
            DaysOfWeek = rule.DaysOfWeek.ToList(),
            IsClosed = rule.IsClosed,
            Labels = rule.Labels.ToList(),
            Reasons = rule.Reasons.ToList(),
            SortOrder = rule.SortOrder,
            TimeRanges = rule.TimeRanges.Select(static timeRange => timeRange.ToResult()).ToList(),
        };
    }

    public static ParkOpeningHoursDateOverrideResult ToResult(this ParkOpeningHoursDateOverride dateOverride)
    {
        return new ParkOpeningHoursDateOverrideResult
        {
            LocalDate = dateOverride.LocalDate,
            IsClosed = dateOverride.IsClosed,
            Labels = dateOverride.Labels.ToList(),
            Reasons = dateOverride.Reasons.ToList(),
            TimeRanges = dateOverride.TimeRanges.Select(static timeRange => timeRange.ToResult()).ToList(),
        };
    }

    public static ParkOpeningHoursTimeRangeResult ToResult(this ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkOpeningHoursTimeRangeResult
        {
            OpensAt = timeRange.OpensAt,
            ClosesAt = timeRange.ClosesAt,
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }
}
