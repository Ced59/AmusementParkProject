using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripPreferenceSummaryQueryHandler
    : IQueryHandler<GetTripPreferenceSummaryQuery, ApplicationResult<TripPreferenceSummaryResult>>
{
    private readonly TripPreferenceSummaryService service;

    public GetTripPreferenceSummaryQueryHandler(TripPreferenceSummaryService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripPreferenceSummaryResult>> HandleAsync(
        GetTripPreferenceSummaryQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
