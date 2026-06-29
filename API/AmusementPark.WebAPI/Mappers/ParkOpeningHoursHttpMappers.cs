using System.Globalization;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkOpeningHours;
using AmusementPark.Application.Features.ParkOpeningHours.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ParkOpeningHours;

namespace AmusementPark.WebAPI.Mappers;

internal static class ParkOpeningHoursHttpMappers
{
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm";

    public static ApplicationResult<ParkOpeningHoursSchedule> ToDomainResult(this ParkOpeningHoursScheduleDto dto, string parkId)
    {
        ArgumentNullException.ThrowIfNull(dto);

        Dictionary<string, List<string>> errors = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        ParkOpeningHoursSchedule schedule = ToDomain(dto, parkId, errors);
        if (errors.Count > 0)
        {
            Dictionary<string, IReadOnlyCollection<string>> validationErrors = errors.ToDictionary(
                static item => item.Key,
                static item => (IReadOnlyCollection<string>)item.Value,
                StringComparer.Ordinal);

            return ApplicationResult<ParkOpeningHoursSchedule>.Failure(ParkOpeningHoursApplicationErrors.InvalidSchedule(validationErrors));
        }

        return ApplicationResult<ParkOpeningHoursSchedule>.Success(schedule);
    }

    private static ParkOpeningHoursSchedule ToDomain(
        ParkOpeningHoursScheduleDto dto,
        string parkId,
        Dictionary<string, List<string>> errors)
    {
        IReadOnlyCollection<ParkOpeningHoursRuleDto> regularRules = dto.RegularRules ?? Array.Empty<ParkOpeningHoursRuleDto>();
        IReadOnlyCollection<ParkOpeningHoursDateOverrideDto> dateOverrides = dto.DateOverrides ?? Array.Empty<ParkOpeningHoursDateOverrideDto>();

        return new ParkOpeningHoursSchedule
        {
            ParkId = parkId.Trim(),
            TimeZoneId = dto.TimeZoneId?.Trim() ?? string.Empty,
            SourceUrl = NormalizeOptionalString(dto.SourceUrl),
            Notes = NormalizeOptionalString(dto.Notes),
            LastVerifiedAtUtc = dto.LastVerifiedAtUtc,
            RegularRules = regularRules.Select((rule, index) => rule.ToDomain(errors, $"regularRules[{index}]")).ToList(),
            DateOverrides = dateOverrides.Select((dateOverride, index) => dateOverride.ToDomain(errors, $"dateOverrides[{index}]")).ToList(),
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

    private static ParkOpeningHoursRule ToDomain(
        this ParkOpeningHoursRuleDto dto,
        Dictionary<string, List<string>> errors,
        string fieldPrefix)
    {
        return new ParkOpeningHoursRule
        {
            Id = NormalizeOptionalString(dto.Id),
            StartDate = ParseDateOrDefault(dto.StartDate),
            EndDate = ParseDateOrDefault(dto.EndDate),
            DaysOfWeek = ParseDaysOfWeek(dto.DaysOfWeek, $"{fieldPrefix}.daysOfWeek", errors),
            IsClosed = dto.IsClosed,
            Label = NormalizeOptionalString(dto.Label),
            Reason = NormalizeOptionalString(dto.Reason),
            SortOrder = dto.SortOrder,
            TimeRanges = (dto.TimeRanges ?? Array.Empty<ParkOpeningHoursTimeRangeDto>())
                .Select((timeRange, index) => timeRange.ToDomain(errors, $"{fieldPrefix}.timeRanges[{index}]"))
                .ToList(),
        };
    }

    private static ParkOpeningHoursDateOverride ToDomain(
        this ParkOpeningHoursDateOverrideDto dto,
        Dictionary<string, List<string>> errors,
        string fieldPrefix)
    {
        return new ParkOpeningHoursDateOverride
        {
            LocalDate = ParseDateOrDefault(dto.LocalDate),
            IsClosed = dto.IsClosed,
            Label = NormalizeOptionalString(dto.Label),
            Reason = NormalizeOptionalString(dto.Reason),
            TimeRanges = (dto.TimeRanges ?? Array.Empty<ParkOpeningHoursTimeRangeDto>())
                .Select((timeRange, index) => timeRange.ToDomain(errors, $"{fieldPrefix}.timeRanges[{index}]"))
                .ToList(),
        };
    }

    private static ParkOpeningHoursTimeRange ToDomain(
        this ParkOpeningHoursTimeRangeDto dto,
        Dictionary<string, List<string>> errors,
        string fieldPrefix)
    {
        bool opensAtParsed = TryParseTime(dto.OpensAt, out TimeOnly opensAt);
        bool closesAtParsed = TryParseTime(dto.ClosesAt, out TimeOnly closesAt);
        TimeOnly? lastAdmissionAt = null;

        if (!opensAtParsed)
        {
            AddError(errors, $"{fieldPrefix}.opensAt", $"Time must use the {TimeFormat} format.");
        }

        if (!closesAtParsed)
        {
            AddError(errors, $"{fieldPrefix}.closesAt", $"Time must use the {TimeFormat} format.");
        }

        if (!string.IsNullOrWhiteSpace(dto.LastAdmissionAt))
        {
            if (TryParseTime(dto.LastAdmissionAt, out TimeOnly parsedLastAdmissionAt))
            {
                lastAdmissionAt = parsedLastAdmissionAt;
            }
            else
            {
                AddError(errors, $"{fieldPrefix}.lastAdmissionAt", $"Time must use the {TimeFormat} format.");
            }
        }

        return new ParkOpeningHoursTimeRange
        {
            OpensAt = opensAtParsed && closesAtParsed ? opensAt : default,
            ClosesAt = opensAtParsed && closesAtParsed ? closesAt : default,
            ClosesNextDay = dto.ClosesNextDay,
            LastAdmissionAt = lastAdmissionAt,
            LastAdmissionNextDay = dto.LastAdmissionNextDay,
        };
    }

    private static List<DayOfWeek> ParseDaysOfWeek(
        IReadOnlyCollection<string>? values,
        string fieldPath,
        Dictionary<string, List<string>> errors)
    {
        List<DayOfWeek> days = new List<DayOfWeek>();
        IReadOnlyCollection<string> rawValues = values ?? Array.Empty<string>();

        foreach (string? value in rawValues)
        {
            string normalizedValue = value?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(normalizedValue)
                || !Enum.TryParse(normalizedValue, true, out DayOfWeek parsed)
                || !Enum.IsDefined(typeof(DayOfWeek), parsed))
            {
                string displayValue = string.IsNullOrWhiteSpace(normalizedValue) ? "<empty>" : normalizedValue;
                AddError(errors, fieldPath, $"Invalid weekday value '{displayValue}'. Allowed values are {string.Join(", ", Enum.GetNames<DayOfWeek>())}.");
                continue;
            }

            days.Add(parsed);
        }

        return days;
    }

    private static void AddError(Dictionary<string, List<string>> errors, string fieldPath, string message)
    {
        if (!errors.TryGetValue(fieldPath, out List<string>? messages))
        {
            messages = new List<string>();
            errors[fieldPath] = messages;
        }

        messages.Add(message);
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
