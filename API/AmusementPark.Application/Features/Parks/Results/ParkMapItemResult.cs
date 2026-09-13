using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Results;

public sealed class ParkMapItemResult
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public ParkItemCategory Category { get; init; }

    public ParkItemType Type { get; init; }

    public string? Subtype { get; init; }

    public string? ZoneId { get; init; }

    public IReadOnlyCollection<LocalizedText> Descriptions { get; init; } = Array.Empty<LocalizedText>();

    public ParkMapAttractionDetailsResult? AttractionDetails { get; init; }

    public double Latitude { get; init; }

    public double Longitude { get; init; }
}
