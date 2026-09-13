using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ContextualBlocks.Contracts;

public sealed class ContextualParkItemLocationBlock
{
    public string ParkId { get; init; } = string.Empty;

    public string ParkItemId { get; init; } = string.Empty;

    public string? ZoneId { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
