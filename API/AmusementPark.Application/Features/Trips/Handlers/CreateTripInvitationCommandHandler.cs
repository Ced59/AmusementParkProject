using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class CreateTripInvitationCommandHandler
    : ICommandHandler<CreateTripInvitationCommand, ApplicationResult<TripInvitationCreationResult>>
{
    private readonly TripInvitationService service;

    public CreateTripInvitationCommandHandler(TripInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripInvitationCreationResult>> HandleAsync(
        CreateTripInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.CreateAsync(
            command.UserId,
            command.TripPlanId,
            command.ClientOperationId,
            command.Input,
            cancellationToken);
    }
}
