namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripProgramDayEvidenceDto
{
    public DateOnly LocalDate { get; init; }

    public string ParkId { get; init; } = string.Empty;

    public string? ParkName { get; init; }

    public bool IsParkAvailable { get; init; }

    public string? ParkStatus { get; init; }

    public string OpeningState { get; init; } = string.Empty;

    public string? OpeningHoursSourceUrl { get; init; }

    public DateTime? OpeningHoursVerifiedAtUtc { get; init; }

    public DateTime DayPlanUpdatedAtUtc { get; init; }
}
