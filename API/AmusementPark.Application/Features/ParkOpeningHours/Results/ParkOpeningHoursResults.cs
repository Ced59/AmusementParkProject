using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkOpeningHours.Results;

public sealed class ParkOpeningHoursScheduleResult
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? Notes { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public IReadOnlyCollection<ParkOpeningHoursRuleResult> RegularRules { get; init; } = Array.Empty<ParkOpeningHoursRuleResult>();

    public IReadOnlyCollection<ParkOpeningHoursDateOverrideResult> DateOverrides { get; init; } = Array.Empty<ParkOpeningHoursDateOverrideResult>();
}

public sealed class ParkOpeningHoursRuleResult
{
    public string Id { get; init; } = string.Empty;

    public DateOnly StartDate { get; init; }

    public DateOnly EndDate { get; init; }

    public IReadOnlyCollection<DayOfWeek> DaysOfWeek { get; init; } = Array.Empty<DayOfWeek>();

    public bool IsClosed { get; init; }

    public IReadOnlyCollection<LocalizedText> Labels { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Reasons { get; init; } = Array.Empty<LocalizedText>();

    public int SortOrder { get; init; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeResult> TimeRanges { get; init; } = Array.Empty<ParkOpeningHoursTimeRangeResult>();
}

public sealed class ParkOpeningHoursDateOverrideResult
{
    public DateOnly LocalDate { get; init; }

    public bool IsClosed { get; init; }

    public IReadOnlyCollection<LocalizedText> Labels { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Reasons { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeResult> TimeRanges { get; init; } = Array.Empty<ParkOpeningHoursTimeRangeResult>();
}

public sealed class ParkOpeningHoursTimeRangeResult
{
    public TimeOnly OpensAt { get; init; }

    public TimeOnly ClosesAt { get; init; }

    public bool ClosesNextDay { get; init; }

    public TimeOnly? LastAdmissionAt { get; init; }

    public bool LastAdmissionNextDay { get; init; }
}

public sealed class ParkOpeningHoursCalendarResult
{
    public string ParkId { get; init; } = string.Empty;

    public string TimeZoneId { get; init; } = string.Empty;

    public string? SourceUrl { get; init; }

    public string? Notes { get; init; }

    public DateTime? LastVerifiedAtUtc { get; init; }

    public DateTime UpdatedAtUtc { get; init; }

    public DateOnly? FirstDate { get; init; }

    public DateOnly? LastDate { get; init; }

    public DateOnly FromDate { get; init; }

    public DateOnly ToDate { get; init; }

    public IReadOnlyCollection<ParkOpeningHoursDayResult> Days { get; init; } = Array.Empty<ParkOpeningHoursDayResult>();
}

public sealed class ParkOpeningHoursDayResult
{
    public DateOnly LocalDate { get; init; }

    public bool IsClosed { get; init; }

    public bool IsDefined { get; init; }

    public string SourceKind { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedText> Labels { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Reasons { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeResult> TimeRanges { get; init; } = Array.Empty<ParkOpeningHoursTimeRangeResult>();
}
