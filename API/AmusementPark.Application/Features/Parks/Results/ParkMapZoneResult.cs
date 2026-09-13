using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Results;

public sealed class ParkMapZoneResult
{
    public required string Id { get; init; }

    public required string Name { get; init; }

    public int SortOrder { get; init; }
}
