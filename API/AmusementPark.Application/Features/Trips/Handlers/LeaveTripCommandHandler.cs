using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class LeaveTripCommandHandler : ICommandHandler<LeaveTripCommand, ApplicationResult>
{
    private readonly TripParticipantService service;

    public LeaveTripCommandHandler(TripParticipantService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        LeaveTripCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.LeaveAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
