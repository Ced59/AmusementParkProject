using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripNotificationStateQueryHandler
    : IQueryHandler<GetTripNotificationStateQuery, ApplicationResult<TripNotificationStateResult>>
{
    private readonly TripNotificationService service;

    public GetTripNotificationStateQueryHandler(TripNotificationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripNotificationStateResult>> HandleAsync(
        GetTripNotificationStateQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.GetAsync(query.UserId, query.TripPlanId, cancellationToken);
    }
}
