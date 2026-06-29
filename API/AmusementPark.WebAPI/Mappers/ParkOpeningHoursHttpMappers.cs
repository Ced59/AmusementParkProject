using System.Globalization;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkOpeningHoursHttpMappers
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static ParkOpeningHoursSchedule ToDomain(this ParkOpeningHoursScheduleDto dto, string parkId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new ParkOpeningHoursSchedule
        {
            ParkId = parkId.Trim(),
            TimeZoneId = dto.TimeZoneId?.Trim() ?? string.Empty,
            SourceUrl = NormalizeOptionalString(dto.SourceUrl),
            Notes = NormalizeOptionalString(dto.Notes),
            LastVerifiedAtUtc = dto.LastVerifiedAtUtc,
            RegularRules = dto.RegularRules.Select(static rule => rule.ToDomain()).ToList(),
            DateOverrides = dto.DateOverrides.Select(static dateOverride => dateOverride.ToDomain()).ToList(),
        };
    }

    public static ParkOpeningHoursScheduleDto ToHttp(this ParkOpeningHoursScheduleResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ParkOpeningHoursScheduleDto
        {
            ParkId = result.ParkId,
            TimeZoneId = result.TimeZoneId,
            SourceUrl = result.SourceUrl,
            Notes = result.Notes,
            LastVerifiedAtUtc = result.LastVerifiedAtUtc,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            RegularRules = result.RegularRules.Select(static rule => rule.ToHttp()).ToList(),
            DateOverrides = result.DateOverrides.Select(static dateOverride => dateOverride.ToHttp()).ToList(),
        };
    }

    public static ParkOpeningHoursScheduleDto ToHttp(this ParkOpeningHoursSchedule schedule)
    {
        ArgumentNullException.ThrowIfNull(schedule);

        return new ParkOpeningHoursScheduleDto
        {
            ParkId = schedule.ParkId,
            TimeZoneId = schedule.TimeZoneId,
            SourceUrl = schedule.SourceUrl,
            Notes = schedule.Notes,
            LastVerifiedAtUtc = schedule.LastVerifiedAtUtc,
            CreatedAtUtc = schedule.CreatedAtUtc,
            UpdatedAtUtc = schedule.UpdatedAtUtc,
            RegularRules = schedule.RegularRules.Select(static rule => rule.ToHttp()).ToList(),
            DateOverrides = schedule.DateOverrides.Select(static dateOverride => dateOverride.ToHttp()).ToList(),
        };
    }

    public static ParkOpeningHoursCalendarDto ToHttp(this ParkOpeningHoursCalendarResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new ParkOpeningHoursCalendarDto
        {
            ParkId = result.ParkId,
            TimeZoneId = result.TimeZoneId,
            SourceUrl = result.SourceUrl,
            Notes = result.Notes,
            LastVerifiedAtUtc = result.LastVerifiedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            FirstDate = result.FirstDate.HasValue ? FormatDate(result.FirstDate.Value) : null,
            LastDate = result.LastDate.HasValue ? FormatDate(result.LastDate.Value) : null,
            FromDate = FormatDate(result.FromDate),
            ToDate = FormatDate(result.ToDate),
            Days = result.Days.Select(static day => day.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursRule ToDomain(this ParkOpeningHoursRuleDto dto)
    {
        return new ParkOpeningHoursRule
        {
            Id = NormalizeOptionalString(dto.Id),
            StartDate = ParseDateOrDefault(dto.StartDate),
            EndDate = ParseDateOrDefault(dto.EndDate),
            DaysOfWeek = dto.DaysOfWeek
                .Select(static value => Enum.TryParse(value, true, out DayOfWeek parsed) ? parsed : (DayOfWeek?)null)
                .Where(static value => value.HasValue)
                .Select(static value => value!.Value)
                .ToList(),
            IsClosed = dto.IsClosed,
            Label = NormalizeOptionalString(dto.Label),
            Reason = NormalizeOptionalString(dto.Reason),
            SortOrder = dto.SortOrder,
            TimeRanges = dto.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverride ToDomain(this ParkOpeningHoursDateOverrideDto dto)
    {
        return new ParkOpeningHoursDateOverride
        {
            LocalDate = ParseDateOrDefault(dto.LocalDate),
            IsClosed = dto.IsClosed,
            Label = NormalizeOptionalString(dto.Label),
            Reason = NormalizeOptionalString(dto.Reason),
            TimeRanges = dto.TimeRanges.Select(static timeRange => timeRange.ToDomain()).ToList(),
        };
    }

    private static ParkOpeningHoursTimeRange ToDomain(this ParkOpeningHoursTimeRangeDto dto)
    {
        bool opensAtParsed = TryParseTime(dto.OpensAt, out TimeOnly opensAt);
        bool closesAtParsed = TryParseTime(dto.ClosesAt, out TimeOnly closesAt);
        TimeOnly? lastAdmissionAt = TryParseTime(dto.LastAdmissionAt, out TimeOnly parsedLastAdmissionAt)
            ? parsedLastAdmissionAt
            : null;

        return new ParkOpeningHoursTimeRange
        {
            OpensAt = opensAtParsed && closesAtParsed ? opensAt : default,
            ClosesAt = opensAtParsed && closesAtParsed ? closesAt : default,
            ClosesNextDay = dto.ClosesNextDay,
            LastAdmissionAt = lastAdmissionAt,
            LastAdmissionNextDay = dto.LastAdmissionNextDay,
        };
    }

    private static ParkOpeningHoursRuleDto ToHttp(this ParkOpeningHoursRuleResult result)
    {
        return new ParkOpeningHoursRuleDto
        {
            Id = result.Id,
            StartDate = FormatDate(result.StartDate),
            EndDate = FormatDate(result.EndDate),
            DaysOfWeek = result.DaysOfWeek.Select(static day => day.ToString()).ToList(),
            IsClosed = result.IsClosed,
            Label = result.Label,
            Reason = result.Reason,
            SortOrder = result.SortOrder,
            TimeRanges = result.TimeRanges.Select(static timeRange => timeRange.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverrideDto ToHttp(this ParkOpeningHoursDateOverrideResult result)
    {
        return new ParkOpeningHoursDateOverrideDto
        {
            LocalDate = FormatDate(result.LocalDate),
            IsClosed = result.IsClosed,
            Label = result.Label,
            Reason = result.Reason,
            TimeRanges = result.TimeRanges.Select(static timeRange => timeRange.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursRuleDto ToHttp(this ParkOpeningHoursRule rule)
    {
        return new ParkOpeningHoursRuleDto
        {
            Id = rule.Id,
            StartDate = FormatDate(rule.StartDate),
            EndDate = FormatDate(rule.EndDate),
            DaysOfWeek = rule.DaysOfWeek.Select(static day => day.ToString()).ToList(),
            IsClosed = rule.IsClosed,
            Label = rule.Label,
            Reason = rule.Reason,
            SortOrder = rule.SortOrder,
            TimeRanges = rule.TimeRanges.Select(static timeRange => timeRange.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursDateOverrideDto ToHttp(this ParkOpeningHoursDateOverride dateOverride)
    {
        return new ParkOpeningHoursDateOverrideDto
        {
            LocalDate = FormatDate(dateOverride.LocalDate),
            IsClosed = dateOverride.IsClosed,
            Label = dateOverride.Label,
            Reason = dateOverride.Reason,
            TimeRanges = dateOverride.TimeRanges.Select(static timeRange => timeRange.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursDayDto ToHttp(this ParkOpeningHoursDayResult result)
    {
        return new ParkOpeningHoursDayDto
        {
            LocalDate = FormatDate(result.LocalDate),
            IsClosed = result.IsClosed,
            IsDefined = result.IsDefined,
            SourceKind = result.SourceKind,
            Label = result.Label,
            Reason = result.Reason,
            TimeRanges = result.TimeRanges.Select(static timeRange => timeRange.ToHttp()).ToList(),
        };
    }

    private static ParkOpeningHoursTimeRangeDto ToHttp(this ParkOpeningHoursTimeRangeResult result)
    {
        return new ParkOpeningHoursTimeRangeDto
        {
            OpensAt = FormatTime(result.OpensAt),
            ClosesAt = FormatTime(result.ClosesAt),
            ClosesNextDay = result.ClosesNextDay,
            LastAdmissionAt = result.LastAdmissionAt.HasValue ? FormatTime(result.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = result.LastAdmissionNextDay,
        };
    }

    private static ParkOpeningHoursTimeRangeDto ToHttp(this ParkOpeningHoursTimeRange timeRange)
    {
        return new ParkOpeningHoursTimeRangeDto
        {
            OpensAt = FormatTime(timeRange.OpensAt),
            ClosesAt = FormatTime(timeRange.ClosesAt),
            ClosesNextDay = timeRange.ClosesNextDay,
            LastAdmissionAt = timeRange.LastAdmissionAt.HasValue ? FormatTime(timeRange.LastAdmissionAt.Value) : null,
            LastAdmissionNextDay = timeRange.LastAdmissionNextDay,
        };
    }

    private static DateOnly ParseDateOrDefault(string? value)
    {
        return DateOnly.TryParseExact(value, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out DateOnly parsed)
            ? parsed
            : default;
    }

    private static bool TryParseTime(string? value, out TimeOnly parsed)
    {
        return TimeOnly.TryParseExact(value, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out parsed);
    }

    private static string FormatDate(DateOnly date)
    {
        return date.ToString(DateFormat, CultureInfo.InvariantCulture);
    }

    private static string FormatTime(TimeOnly time)
    {
        return time.ToString(TimeFormat, CultureInfo.InvariantCulture);
    }

    private static string? NormalizeOptionalString(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
