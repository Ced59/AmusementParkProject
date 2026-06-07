using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

/// <summary>
/// Contrat HTTP de mise a jour rapide des champs metier des park items.
/// </summary>
public sealed class ParkItemBulkFieldsUpdateDto
{
    public List<string> Ids { get; set; } = new();

    public bool UpdateZone { get; set; }

    public string? ZoneId { get; set; }

    public ParkItemCategoryDto? Category { get; set; }

    public ParkItemTypeDto? Type { get; set; }

    public bool UpdateManufacturer { get; set; }

    public string? ManufacturerId { get; set; }

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }
}
