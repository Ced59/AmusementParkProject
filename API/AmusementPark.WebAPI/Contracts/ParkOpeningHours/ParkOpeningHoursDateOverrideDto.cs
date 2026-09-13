using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursDateOverrideDto
{
    public string LocalDate { get; set; } = string.Empty;

    public bool IsClosed { get; set; }

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<LocalizedTextDto> Reasons { get; set; } = Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}
