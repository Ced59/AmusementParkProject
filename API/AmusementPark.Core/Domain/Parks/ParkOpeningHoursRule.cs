using AmusementPark.Core.Localization;

namespace AmusementPark.Core.Domain.Parks;

public sealed class ParkOpeningHoursRule
{
    public string? Id { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public List<DayOfWeek> DaysOfWeek { get; set; } = new();

    public bool IsClosed { get; set; }

    public List<LocalizedText> Labels { get; set; } = new();

    public List<LocalizedText> Reasons { get; set; } = new();

    public int SortOrder { get; set; }

    public List<ParkOpeningHoursTimeRange> TimeRanges { get; set; } = new();
}
