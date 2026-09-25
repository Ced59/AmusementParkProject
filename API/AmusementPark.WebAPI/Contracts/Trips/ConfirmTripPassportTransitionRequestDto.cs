namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed class ConfirmTripPassportTransitionRequestDto
{
    public IReadOnlyCollection<string> ParkItemIds { get; init; } = Array.Empty<string>();
}
