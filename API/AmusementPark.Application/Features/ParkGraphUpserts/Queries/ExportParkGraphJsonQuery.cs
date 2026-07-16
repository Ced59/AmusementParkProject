using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Queries;

public sealed record ExportParkGraphJsonQuery(
    string ParkId,
    IReadOnlyCollection<ParkGraphExportSection>? Sections = null) : IQuery<ApplicationResult<ParkGraphJsonExportResult>>;

public sealed record ExportStandaloneAttractionGraphJsonQuery(string StandaloneAttractionId)
    : IQuery<ApplicationResult<ParkGraphJsonExportResult>>;
