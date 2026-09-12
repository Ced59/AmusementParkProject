using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkOpeningHours.Results;

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
