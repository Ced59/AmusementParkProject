using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkOpeningHours.Results;

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
