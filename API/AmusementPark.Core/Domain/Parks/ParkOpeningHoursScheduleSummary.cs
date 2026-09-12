namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursScheduleSummary
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public DateOnly? FirstDate { get; init; }

    public DateOnly? LastDate { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public bool HasScheduleData { get; init; }

    public bool HasDateOverrides { get; init; }

    public IReadOnlyCollection<ParkOpeningHoursCoverageSegmentSummary> CoverageSegments { get; init; } = Array.Empty<ParkOpeningHoursCoverageSegmentSummary>();
}
