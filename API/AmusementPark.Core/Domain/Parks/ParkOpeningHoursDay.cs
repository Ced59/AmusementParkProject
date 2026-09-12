using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursDay
{
    public DateOnly LocalDate { get; init; }

    public bool IsClosed { get; init; }

    public bool IsDefined { get; init; }

    public string SourceKind { get; init; } = string.Empty;

    public IReadOnlyCollection<LocalizedText> Labels { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<LocalizedText> Reasons { get; init; } = Array.Empty<LocalizedText>();

    public IReadOnlyCollection<ParkOpeningHoursTimeRange> TimeRanges { get; init; } = Array.Empty<ParkOpeningHoursTimeRange>();
}
