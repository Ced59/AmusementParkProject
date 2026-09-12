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
