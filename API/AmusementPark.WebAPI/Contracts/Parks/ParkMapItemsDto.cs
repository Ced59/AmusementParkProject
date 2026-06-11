namespace AmusementPark.WebAPI.Contracts.Parks;

/// <summary>
/// Contrat HTTP optimisé pour la carte détaillée publique d'un parc.
/// </summary>
public sealed class ParkMapItemsDto
{
    public required ParkDto Park { get; set; }

    public IReadOnlyCollection<ParkMapItemDto> Items { get; set; } = Array.Empty<ParkMapItemDto>();

    public IReadOnlyCollection<ParkMapZoneDto> Zones { get; set; } = Array.Empty<ParkMapZoneDto>();
}

public sealed class ParkMapItemDto
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public required string Category { get; set; }

    public required string Type { get; set; }

    public string? Subtype { get; set; }

    public string? ZoneId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }
}

public sealed class ParkMapZoneDto
{
    public required string Id { get; set; }

    public required string Name { get; set; }

    public int SortOrder { get; set; }
}
