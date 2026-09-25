namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripExportDayBlockDto
{
    public string Type { get; set; } = string.Empty;

    public string Title { get; set; } = string.Empty;

    public string? Details { get; set; }

    public string? LocalTime { get; set; }
}
