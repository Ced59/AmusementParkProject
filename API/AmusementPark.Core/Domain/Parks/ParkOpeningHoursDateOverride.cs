using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursDateOverride
{
    public DateOnly LocalDate { get; set; }

    public bool IsClosed { get; set; }

    public List<LocalizedText> Labels { get; set; } = new();

    public List<LocalizedText> Reasons { get; set; } = new();

    public List<ParkOpeningHoursTimeRange> TimeRanges { get; set; } = new();
}
