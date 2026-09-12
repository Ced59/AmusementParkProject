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
