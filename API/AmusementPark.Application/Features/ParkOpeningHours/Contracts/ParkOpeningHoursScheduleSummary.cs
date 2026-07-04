using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Contracts;

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

public sealed class ParkOpeningHoursCoverageSegmentSummary
{
    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }
}

public sealed class ParkOpeningHoursAdminCoverage
{
    public ParkOpeningHoursAdminStatus Status { get; init; } = ParkOpeningHoursAdminStatus.NotConfigured;

    public int? CompleteForDays { get; init; }

    public DateOnly? CompleteUntilDate { get; init; }

    public int WarningThresholdDays { get; init; } = 30;
}
