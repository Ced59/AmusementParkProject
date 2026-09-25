using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ExportTripPlanQueryHandler
    : IQueryHandler<ExportTripPlanQuery, ApplicationResult<TripExportResult>>
{
    private readonly TripExportService service;

    public ExportTripPlanQueryHandler(TripExportService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripExportResult>> HandleAsync(
        ExportTripPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ExportAsync(
            query.UserId,
            query.TripPlanId,
            query.ExportRequestId,
            cancellationToken);
    }
}
