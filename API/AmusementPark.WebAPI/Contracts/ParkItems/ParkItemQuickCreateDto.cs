using System.ComponentModel.DataAnnotations;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP dedie a la creation rapide d'un park item depuis le workbench admin.
/// </summary>
public sealed class ParkItemQuickCreateDto
{
    [Required]
    public string ParkId { get; set; } = string.Empty;

    public string? ZoneId { get; set; }

    [Required]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    public ParkItemCategoryDto? Category { get; set; }

    public ParkItemTypeDto? Type { get; set; }

    public string? ManufacturerId { get; set; }

    public double? Latitude { get; set; }

    public double? Longitude { get; set; }

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }
}
