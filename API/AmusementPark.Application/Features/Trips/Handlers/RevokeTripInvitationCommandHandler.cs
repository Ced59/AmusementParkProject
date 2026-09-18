using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class RevokeTripInvitationCommandHandler
    : ICommandHandler<RevokeTripInvitationCommand, ApplicationResult>
{
    private readonly TripInvitationService service;

    public RevokeTripInvitationCommandHandler(TripInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        RevokeTripInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.RevokeAsync(
            command.UserId,
            command.TripPlanId,
            command.InvitationId,
            command.ExpectedInvitationVersion,
            command.ClientOperationId,
            cancellationToken);
    }
}
