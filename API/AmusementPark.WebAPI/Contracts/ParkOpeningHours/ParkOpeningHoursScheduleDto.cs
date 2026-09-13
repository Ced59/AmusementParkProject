using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursScheduleDto
{
    public string ParkId { get; set; } = string.Empty;

    public string TimeZoneId { get; set; } = string.Empty;

    public string? SourceUrl { get; set; }

    public string? Notes { get; set; }

    public DateTime? LastVerifiedAtUtc { get; set; }

    public DateTime? CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<ParkOpeningHoursRuleDto> RegularRules { get; set; } = Array.Empty<ParkOpeningHoursRuleDto>();

    public IReadOnlyCollection<ParkOpeningHoursDateOverrideDto> DateOverrides { get; set; } = Array.Empty<ParkOpeningHoursDateOverrideDto>();
}
