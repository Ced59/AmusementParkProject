using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP détaillé d'un park item.
/// </summary>
public sealed class ParkItemDto
{
    public string? Id { get; set; }

    public string ParkId { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    public string Name { get; set; } = string.Empty;

    public ParkItemCategoryDto Category { get; set; }

    public ParkItemTypeDto Type { get; set; }

    public string? Subtype { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public List<LocalizedTextDto> Descriptions { get; set; } = new();

    public AttractionDetailsDto? AttractionDetails { get; set; }

    public AttractionLocationsDto? AttractionLocations { get; set; }

    public bool IsVisible { get; set; } = true;
}
