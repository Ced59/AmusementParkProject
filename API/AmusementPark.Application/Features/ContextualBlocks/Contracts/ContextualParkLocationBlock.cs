using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualParkLocationBlock
{
    public string ParkId { get; init; } = string.Empty;

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
