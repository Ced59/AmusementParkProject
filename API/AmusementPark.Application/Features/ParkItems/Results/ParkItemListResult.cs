using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkItems.Results;

public sealed class ParkItemListResult
{
    public required ParkItem Item { get; init; }

    public string? MainImageId { get; init; }
}
