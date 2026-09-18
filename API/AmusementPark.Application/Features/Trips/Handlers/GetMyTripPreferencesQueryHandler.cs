using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetMyTripPreferencesQueryHandler
    : IQueryHandler<GetMyTripPreferencesQuery, ApplicationResult<TripPreferenceBoardResult>>
{
    private readonly TripPreferenceService service;

    public GetMyTripPreferencesQueryHandler(TripPreferenceService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPreferenceBoardResult>> HandleAsync(
        GetMyTripPreferencesQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
