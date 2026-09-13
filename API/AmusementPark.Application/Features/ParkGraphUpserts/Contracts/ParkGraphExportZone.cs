using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportZone
{
    public string Key { get; init; } = string.Empty;

    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public List<LocalizedText> Names { get; init; } = new List<LocalizedText>();

    public string? Slug { get; init; }

    public List<LocalizedText> Descriptions { get; init; } = new List<LocalizedText>();

    public bool IsVisible { get; init; }

    public int SortOrder { get; init; }

    public double? Latitude { get; init; }

    public double? Longitude { get; init; }
}
