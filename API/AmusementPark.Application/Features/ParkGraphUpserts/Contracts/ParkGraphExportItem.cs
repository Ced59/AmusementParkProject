using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportItem
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public ParkItemCategory Category { get; init; }

    public ParkItemType Type { get; init; }

    public string? Subtype { get; init; }

    public string? ZoneId { get; init; }

    public string? ZoneKey { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public ParkGraphExportAttractionDetails? AttractionDetails { get; init; }

    public AttractionLocations? AttractionLocations { get; init; }

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
