using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportIdentity
{
    public string? ParkId { get; init; }

    public string? Id { get; init; }

    public string? Name { get; init; }

    public string? CountryCode { get; init; }
}
