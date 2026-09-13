using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportReferences
{
    public List<ParkGraphExportFounder> Founders { get; init; } = new List<ParkGraphExportFounder>();

    public List<ParkGraphExportOperator> Operators { get; init; } = new List<ParkGraphExportOperator>();

    public List<ParkGraphExportManufacturer> Manufacturers { get; init; } = new List<ParkGraphExportManufacturer>();
}
