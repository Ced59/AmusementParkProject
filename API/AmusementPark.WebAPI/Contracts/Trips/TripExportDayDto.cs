namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripExportDayDto
{
    public string LocalDate { get; set; } = string.Empty;

    public string? ParkName { get; set; }

    public bool IsParkAvailable { get; set; }

    public string? DesiredArrivalTime { get; set; }

    public string? GroupNote { get; set; }

    public IReadOnlyCollection<TripExportDayBlockDto> Blocks { get; set; } =
        Array.Empty<TripExportDayBlockDto>();
}
