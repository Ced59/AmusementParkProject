namespace AmusementPark.WebAPI.Contracts.Passport;

public sealed class ValidatePassportRideTargetsRequestDto
{
    public string ParkId { get; init; } = string.Empty;

    public IReadOnlyCollection<string?> ParkItemIds { get; init; } =
        Array.Empty<string?>();
}
