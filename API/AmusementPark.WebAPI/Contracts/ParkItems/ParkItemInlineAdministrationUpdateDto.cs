using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP dedie aux futures editions inline simples du workbench admin.
/// </summary>
public sealed class ParkItemInlineAdministrationUpdateDto
{
    public string? ZoneId { get; set; }

    public ParkItemCategoryDto? Category { get; set; }

    public ParkItemTypeDto? Type { get; set; }

    public string? ManufacturerId { get; set; }

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }
}
