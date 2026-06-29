namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursScheduleDto
{
    public string ParkId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursRuleDto> RegularRules { get; set; } = Array.Empty<ParkOpeningHoursRuleDto>();

    public IReadOnlyCollection<ParkOpeningHoursDateOverrideDto> DateOverrides { get; set; } = Array.Empty<ParkOpeningHoursDateOverrideDto>();
}

public sealed class ParkOpeningHoursRuleDto
{
    public string? Id { get; set; }

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public IReadOnlyCollection<string> DaysOfWeek { get; set; } = Array.Empty<string>();

    public bool IsClosed { get; set; }

    public string? Label { get; set; }

    public string? Reason { get; set; }

    public int SortOrder { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}

public sealed class ParkOpeningHoursDateOverrideDto
{
    public string LocalDate { get; set; } = string.Empty;

    public bool IsClosed { get; set; }

    public string? Label { get; set; }

    public string? Reason { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}

public sealed class ParkOpeningHoursTimeRangeDto
{
    public string OpensAt { get; set; } = string.Empty;

    public string ClosesAt { get; set; } = string.Empty;

    public bool ClosesNextDay { get; set; }

    public string? LastAdmissionAt { get; set; }

    public bool LastAdmissionNextDay { get; set; }
}

public sealed class ParkOpeningHoursCalendarDto
{
    public string ParkId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? FirstDate { get; set; }

    public string? LastDate { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkOpeningHoursDayDto> Days { get; set; } = Array.Empty<ParkOpeningHoursDayDto>();
}

public sealed class ParkOpeningHoursDayDto
{
    public string LocalDate { get; set; } = string.Empty;

    public bool IsClosed { get; set; }

    public bool IsDefined { get; set; }

    public string SourceKind { get; set; } = string.Empty;

    public string? Label { get; set; }

    public string? Reason { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}
