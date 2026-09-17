using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetMyTripPlanQueryHandler
    : IQueryHandler<GetMyTripPlanQuery, ApplicationResult<TripPlanResult>>
{
    private readonly TripPlanLifecycleService service;

    public GetMyTripPlanQueryHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPlanResult>> HandleAsync(
        GetMyTripPlanQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
