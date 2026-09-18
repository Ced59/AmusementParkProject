using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class AcceptTripInvitationCommandHandler : ICommandHandler<
    AcceptTripInvitationCommand,
    ApplicationResult<TripInvitationDecisionResult>>
{
    private readonly TripAdmissionService service;

    public AcceptTripInvitationCommandHandler(TripAdmissionService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripInvitationDecisionResult>> HandleAsync(
        AcceptTripInvitationCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.AcceptAsync(
            command.UserId,
            command.Token,
            command.ClientOperationId,
            cancellationToken);
    }
}
