namespace AmusementPark.WebAPI.Contracts.ParkFit;

public sealed class ParkFitDataQualityItemDto
{
    public string ParkItemId { get; init; } = string.Empty;

    public string ParkItemName { get; init; } = string.Empty;

    public IReadOnlyCollection<string> Issues { get; init; } = Array.Empty<string>();
}
