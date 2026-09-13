using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Results;

public sealed class ParkItemBulkCreatePreviewRow
{
    public int RowNumber { get; init; }

    public string Name { get; init; } = string.Empty;

    public ParkItemCategory Category { get; init; }

    public ParkItemType Type { get; init; }

    public string? ZoneId { get; init; }

    public string? ZoneName { get; init; }

    public string? ManufacturerId { get; init; }

    public string? ManufacturerName { get; init; }

    public bool IsVisible { get; init; }

    public AdminReviewStatus AdminReviewStatus { get; init; }

    public string? DescriptionFr { get; init; }

    public bool CanApply { get; init; }

    public IReadOnlyCollection<string> Errors { get; init; } = Array.Empty<string>();

    public IReadOnlyCollection<string> Warnings { get; init; } = Array.Empty<string>();
}
