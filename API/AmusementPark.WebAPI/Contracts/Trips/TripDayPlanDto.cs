namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripDayPlanDto
{
    public string DayPlanId { get; set; } = string.Empty;
    public string LocalDate { get; set; } = string.Empty;
    public string ParkCandidateId { get; set; } = string.Empty;
    public string ParkId { get; set; } = string.Empty;
    public string? ParkName { get; set; }
    public bool IsParkAvailable { get; set; }
    public string? DesiredArrivalTime { get; set; }
    public string? GroupNote { get; set; }
    public IReadOnlyCollection<TripDayBlockDto> Blocks { get; set; } = Array.Empty<TripDayBlockDto>();
    public long Version { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
