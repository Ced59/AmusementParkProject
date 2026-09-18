using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ListTripInvitationsQueryHandler
    : IQueryHandler<ListTripInvitationsQuery,
        ApplicationResult<TripInvitationListResult>>
{
    private readonly TripInvitationService service;

    public ListTripInvitationsQueryHandler(TripInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripInvitationListResult>> HandleAsync(
        ListTripInvitationsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.ListAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
