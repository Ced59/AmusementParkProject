using System.Collections.Generic;
using AmusementPark.WebAPI.Contracts.Common;

namespace AmusementPark.WebAPI.Contracts.ParkItems;

public sealed class ParkItemBulkCreateDraftDto
{
    public int RowNumber { get; set; }

    public string? Name { get; set; }

    public ParkItemCategoryDto? Category { get; set; }

    public ParkItemTypeDto? Type { get; set; }

    public string? ZoneId { get; set; }

    public string? ZoneName { get; set; }

    public string? ManufacturerId { get; set; }

    public string? ManufacturerName { get; set; }

    public bool? IsVisible { get; set; }

    public AdminReviewStatusDto? AdminReviewStatus { get; set; }

    public string? DescriptionFr { get; set; }
}
