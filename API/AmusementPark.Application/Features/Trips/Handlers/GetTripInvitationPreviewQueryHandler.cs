using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripInvitationPreviewQueryHandler
    : IQueryHandler<GetTripInvitationPreviewQuery, ApplicationResult<TripInvitationPreviewResult>>
{
    private readonly TripInvitationService service;

    public GetTripInvitationPreviewQueryHandler(TripInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripInvitationPreviewResult>> HandleAsync(
        GetTripInvitationPreviewQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        return this.service.PreviewAsync(query.Token, cancellationToken);
    }
}
