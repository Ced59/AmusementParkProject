using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursCalendarDto
{
    public string ParkId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public string? FirstDate { get; set; }

    public string? LastDate { get; set; }

    public string FromDate { get; set; } = string.Empty;

    public string ToDate { get; set; } = string.Empty;

    public IReadOnlyCollection<ParkOpeningHoursDayDto> Days { get; set; } = Array.Empty<ParkOpeningHoursDayDto>();
}
