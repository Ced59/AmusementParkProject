using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportDocument
{
    public string DocumentType { get; init; } = "AmusementParkParkGraphUpsert";

    public string SchemaVersion { get; init; } = "2026-09-05";

    public string Mode { get; init; } = "merge";

    public ParkGraphExportIdentity Identity { get; init; } = new ParkGraphExportIdentity();

    public ParkGraphExportReferences References { get; init; } = new ParkGraphExportReferences();

    public ParkGraphExportPark Park { get; init; } = new ParkGraphExportPark();

    public List<ParkGraphExportZone> Zones { get; init; } = new List<ParkGraphExportZone>();

    public List<ParkGraphExportItem> Items { get; init; } = new List<ParkGraphExportItem>();

    public List<ParkGraphExportImage> Images { get; init; } = new List<ParkGraphExportImage>();

    public ParkGraphExportOpeningHours? OpeningHours { get; init; }

    public ParkGraphExportHistory History { get; init; } = new ParkGraphExportHistory();

    public ParkGraphExportMetadata Metadata { get; init; } = new ParkGraphExportMetadata();
}
