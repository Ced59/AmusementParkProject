using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ChangeTripParticipantRoleCommandHandler : ICommandHandler<
    ChangeTripParticipantRoleCommand,
    ApplicationResult<TripParticipantListResult>>
{
    private readonly TripParticipantService service;

    public ChangeTripParticipantRoleCommandHandler(TripParticipantService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripParticipantListResult>> HandleAsync(
        ChangeTripParticipantRoleCommand command,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.ChangeRoleAsync(
            command.UserId,
            command.TripPlanId,
            command.MemberId,
            command.Role,
            command.ExpectedVersion,
            cancellationToken);
    }
}
