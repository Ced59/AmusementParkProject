using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class TransferTripOwnershipCommandHandler : ICommandHandler<
    TransferTripOwnershipCommand,
    ApplicationResult<TripParticipantListResult>>
{
    private readonly TripParticipantService service;

    public TransferTripOwnershipCommandHandler(TripParticipantService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripParticipantListResult>> HandleAsync(
        TransferTripOwnershipCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.TransferOwnershipAsync(
            command.UserId,
            command.TripPlanId,
            command.NewOwnerMemberId,
            command.PreviousOwnerRole,
            command.ExpectedVersion,
            cancellationToken);
    }
}
