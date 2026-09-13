using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkOpeningHours;

public sealed class ParkOpeningHoursTimeRangeDto
{
    public string OpensAt { get; set; } = string.Empty;

    public string ClosesAt { get; set; } = string.Empty;

    public bool ClosesNextDay { get; set; }

    public string? LastAdmissionAt { get; set; }

    public bool LastAdmissionNextDay { get; set; }
}
