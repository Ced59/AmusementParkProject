using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Results;

/// <summary>
/// Résultat optimisé pour la page carte d'un parc.
/// </summary>
public sealed class ParkMapItemsResult
{
    public required Park Park { get; init; }

    public IReadOnlyCollection<ParkMapItemResult> Items { get; init; } = Array.Empty<ParkMapItemResult>();

    public IReadOnlyCollection<ParkMapUnlocatedItemResult> UnlocatedItems { get; init; } = Array.Empty<ParkMapUnlocatedItemResult>();

    public IReadOnlyCollection<ParkMapZoneResult> Zones { get; init; } = Array.Empty<ParkMapZoneResult>();
}

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

public sealed class ParkMapUnlocatedItemResult
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public ParkItemCategory Category { get; init; }

    public ParkItemType Type { get; init; }

    public string? Subtype { get; init; }

    public string? ZoneId { get; init; }

    public IReadOnlyCollection<LocalizedText> Descriptions { get; init; } = Array.Empty<LocalizedText>();

    public ParkMapAttractionDetailsResult? AttractionDetails { get; init; }
}

public sealed class ParkMapAttractionDetailsResult
{
    public string? ManufacturerId { get; init; }

    public string? Model { get; init; }

    public string? Status { get; init; }
}

public sealed class ParkMapZoneResult
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public int SortOrder { get; init; }
}
