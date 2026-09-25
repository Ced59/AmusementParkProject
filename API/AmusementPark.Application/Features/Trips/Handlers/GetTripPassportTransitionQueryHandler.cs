using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripPassportTransitionQueryHandler
    : IQueryHandler<GetTripPassportTransitionQuery,
        ApplicationResult<TripPassportTransitionResult>>
{
    private readonly TripPassportTransitionReader reader;

    public GetTripPassportTransitionQueryHandler(TripPassportTransitionReader reader)
    {
        this.reader = reader ?? throw new ArgumentNullException(nameof(reader));
    }

    public Task<ApplicationResult<TripPassportTransitionResult>> HandleAsync(
        GetTripPassportTransitionQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.reader.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
