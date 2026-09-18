using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ListTripParticipantsQueryHandler : IQueryHandler<
    ListTripParticipantsQuery,
    ApplicationResult<TripParticipantListResult>>
{
    private readonly TripParticipantService service;

    public ListTripParticipantsQueryHandler(TripParticipantService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripParticipantListResult>> HandleAsync(
        ListTripParticipantsQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ListAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
