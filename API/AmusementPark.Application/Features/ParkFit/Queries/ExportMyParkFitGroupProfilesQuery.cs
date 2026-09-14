using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.ParkFit.Results;

namespace AmusementPark.Application.Features.ParkFit.Queries;

public sealed record ExportMyParkFitGroupProfilesQuery(string OwnerUserId)
    : IQuery<ApplicationResult<ParkFitGroupProfileExportResult>>;
