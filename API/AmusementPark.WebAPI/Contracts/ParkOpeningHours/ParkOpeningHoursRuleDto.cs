using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursRuleDto
{
    public string? Id { get; set; }

    public string StartDate { get; set; } = string.Empty;

    public string EndDate { get; set; } = string.Empty;

    public IReadOnlyCollection<string> DaysOfWeek { get; set; } = Array.Empty<string>();

    public bool IsClosed { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<LocalizedTextDto> Reasons { get; set; } = Array.Empty<LocalizedTextDto>();

    public int SortOrder { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}
