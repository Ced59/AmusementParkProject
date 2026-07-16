namespace AmusementPark.Application.Features.StandaloneAttractions.Contracts;

public sealed class StandaloneAttractionMigrationRequest
{
    public string LegacyParkId { get; init; } = string.Empty;

    public string? LegacyParkItemId { get; init; }

    public string? TargetStandaloneAttractionId { get; init; }

    public bool RetireLegacyPark { get; init; } = true;

    public bool RetireLegacyParkItem { get; init; } = true;
}

