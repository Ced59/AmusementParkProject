namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitOpeningTimeRangeDto
{
    public string OpensAt { get; init; } = string.Empty;

    public string ClosesAt { get; init; } = string.Empty;

    public bool ClosesNextDay { get; init; }

    public string? LastAdmissionAt { get; init; }

    public bool LastAdmissionNextDay { get; init; }
}
