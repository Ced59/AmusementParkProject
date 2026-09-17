using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ListMyTripPlansQueryHandler
    : IQueryHandler<ListMyTripPlansQuery, ApplicationResult<IReadOnlyCollection<TripPlanResult>>>
{
    private readonly TripPlanLifecycleService service;

    public ListMyTripPlansQueryHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<IReadOnlyCollection<TripPlanResult>>> HandleAsync(
        ListMyTripPlansQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ListAsync(query.UserId, cancellationToken);
    }
}
