using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripProgramQueryHandler
    : IQueryHandler<GetTripProgramQuery, ApplicationResult<TripProgramResult>>
{
    private readonly TripProgramService service;

    public GetTripProgramQueryHandler(TripProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripProgramResult>> HandleAsync(
        GetTripProgramQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
