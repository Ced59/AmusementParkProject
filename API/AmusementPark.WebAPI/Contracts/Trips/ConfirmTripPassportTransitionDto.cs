namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class ConfirmTripPassportTransitionDto
{
    public string VisitId { get; set; } = string.Empty;

    public bool WasReplayed { get; set; }

    public int AddedRideCount { get; set; }
}
