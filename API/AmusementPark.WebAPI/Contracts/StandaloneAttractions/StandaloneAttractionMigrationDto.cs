using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.ParkItems;

namespace AmusementPark.WebAPI.Contracts.StandaloneAttractions;

public sealed class StandaloneAttractionMigrationDto
{
    public string LegacyParkId { get; set; } = string.Empty;

    public string? LegacyParkItemId { get; set; }

    public string? TargetStandaloneAttractionId { get; set; }

    public bool RetireLegacyPark { get; set; } = true;

    public bool RetireLegacyParkItem { get; set; } = true;
}
