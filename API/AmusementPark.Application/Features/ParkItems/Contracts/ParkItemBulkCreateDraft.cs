using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Contracts;

public sealed class ParkItemBulkCreateDraft
{
    public int RowNumber { get; init; }

    public string? Name { get; init; }

    public ParkItemCategory? Category { get; init; }

    public ParkItemType? Type { get; init; }

    public string? ZoneId { get; init; }

    public string? ZoneName { get; init; }

    public string? ManufacturerId { get; init; }

    public string? ManufacturerName { get; init; }

    public bool? IsVisible { get; init; }

    public AdminReviewStatus? AdminReviewStatus { get; init; }

    public string? DescriptionFr { get; init; }
}
