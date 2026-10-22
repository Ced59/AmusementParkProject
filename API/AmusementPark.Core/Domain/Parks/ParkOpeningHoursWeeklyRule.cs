using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Usual weekly hours from a source that does not publish a dated applicability period.
/// These hours do not assert opening or availability on a particular calendar date.
/// </summary>
public sealed class ParkOpeningHoursWeeklyRule
{
    public string? Id { get; set; }

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public bool IsClosed { get; set; }

    public List<LocalizedText> Labels { get; set; } = new();

    public List<LocalizedText> Reasons { get; set; } = new();

    public int SortOrder { get; set; }

    public List<ParkOpeningHoursTimeRange> TimeRanges { get; set; } = new();
}
