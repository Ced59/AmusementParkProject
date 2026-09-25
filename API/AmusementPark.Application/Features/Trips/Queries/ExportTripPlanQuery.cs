using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Queries;

public sealed record ExportTripPlanQuery(
    string UserId,
    string TripPlanId,
    string ExportRequestId)
    : IQuery<ApplicationResult<TripExportResult>>;
