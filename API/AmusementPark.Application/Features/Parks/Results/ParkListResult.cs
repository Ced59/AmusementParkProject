using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.Parks.Results;

public sealed class ParkListResult
{
    public required Park Park { get; init; }

    public int? ParkItemsTotalCount { get; init; }

    public int? ParkItemsVisibleCount { get; init; }
}
