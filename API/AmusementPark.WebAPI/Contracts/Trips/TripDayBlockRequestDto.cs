namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripDayBlockRequestDto
{
    public string? BlockId { get; set; }
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Details { get; set; }
    public string? LocalTime { get; set; }
    public long SortPosition { get; set; }
}
