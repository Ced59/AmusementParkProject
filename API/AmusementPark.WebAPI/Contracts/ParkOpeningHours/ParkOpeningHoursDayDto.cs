using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursDayDto
{
    public string LocalDate { get; set; } = string.Empty;

    public bool IsClosed { get; set; }

    public bool IsDefined { get; set; }

    public string SourceKind { get; set; } = string.Empty;

    public IReadOnlyCollection<LocalizedTextDto> Labels { get; set; } = Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<LocalizedTextDto> Reasons { get; set; } = Array.Empty<LocalizedTextDto>();

    public IReadOnlyCollection<ParkOpeningHoursTimeRangeDto> TimeRanges { get; set; } = Array.Empty<ParkOpeningHoursTimeRangeDto>();
}
