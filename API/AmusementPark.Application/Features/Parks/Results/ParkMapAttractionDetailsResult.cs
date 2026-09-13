using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.Parks.Results;

public sealed class ParkMapAttractionDetailsResult
{
    public string? ManufacturerId { get; init; }

    public string? Model { get; init; }

    public string? Status { get; init; }
}
