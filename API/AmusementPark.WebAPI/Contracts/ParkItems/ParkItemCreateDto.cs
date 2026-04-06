using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP de création d'un park item.
/// </summary>
public sealed class ParkItemCreateDto
{
    [Required]
    public string ParkId { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    [Required]
    [MaxLength(200)]
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
