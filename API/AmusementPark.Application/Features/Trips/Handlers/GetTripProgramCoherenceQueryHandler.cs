using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripProgramCoherenceQueryHandler
    : IQueryHandler<GetTripProgramCoherenceQuery, ApplicationResult<TripProgramCoherenceResult>>
{
    private readonly TripProgramCoherenceService service;

    public GetTripProgramCoherenceQueryHandler(TripProgramCoherenceService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripProgramCoherenceResult>> HandleAsync(
        GetTripProgramCoherenceQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
