namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class TripPassportTransitionItemDto
{
    public string ParkItemId { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? MainImageId { get; set; }

    public string OwnPreference { get; set; } = string.Empty;

    public string HistoricalConsistency { get; set; } = string.Empty;

    public bool IsPreselected { get; set; }
}
