namespace AmusementPark.WebAPI.Contracts.ParkZones;

public sealed class ParkZoneSummaryCountDto
{
    public string Key { get; set; } = string.Empty;

    public int Count { get; set; }
}
