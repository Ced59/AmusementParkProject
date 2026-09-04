namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class EvaluatePassportVisitRideTargetsRequestDto
{
    public IReadOnlyCollection<string?> ParkItemIds { get; init; } = Array.Empty<string?>();
}
