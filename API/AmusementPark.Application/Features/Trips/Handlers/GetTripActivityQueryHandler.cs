using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripActivityQueryHandler
    : IQueryHandler<GetTripActivityQuery, ApplicationResult<TripActivityPageResult>>
{
    private readonly TripActivityService service;

    public GetTripActivityQueryHandler(TripActivityService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripActivityPageResult>> HandleAsync(
        GetTripActivityQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(
            query.UserId,
            query.TripPlanId,
            query.BeforeSequence,
            cancellationToken);
    }
}
