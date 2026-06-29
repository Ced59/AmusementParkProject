namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursSchedule
{
    public string? Id { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public List<ParkOpeningHoursRule> RegularRules { get; set; } = new();

    public List<ParkOpeningHoursDateOverride> DateOverrides { get; set; } = new();

    public List<ParkOpeningHoursCoverageSegment> CoverageSegments { get; set; } = new();
}

public sealed class ParkOpeningHoursRule
{
    public string? Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public bool IsClosed { get; set; }

    public string? Label { get; set; }

    public string? Reason { get; set; }

    public int SortOrder { get; set; }

    public List<ParkOpeningHoursTimeRange> TimeRanges { get; set; } = new();
}

public sealed class ParkOpeningHoursDateOverride
{
    public DateOnly LocalDate { get; set; }

    public bool IsClosed { get; set; }

    public string? Label { get; set; }

    public string? Reason { get; set; }

    public List<ParkOpeningHoursTimeRange> TimeRanges { get; set; } = new();
}

public sealed class ParkOpeningHoursTimeRange
{
    public TimeOnly OpensAt { get; set; }

    public TimeOnly ClosesAt { get; set; }

    public bool ClosesNextDay { get; set; }

    public TimeOnly? LastAdmissionAt { get; set; }

    public bool LastAdmissionNextDay { get; set; }
}

public sealed class ParkOpeningHoursCoverageSegment
{
    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }
}
