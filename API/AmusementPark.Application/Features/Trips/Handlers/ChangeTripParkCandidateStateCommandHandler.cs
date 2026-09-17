using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class ChangeTripParkCandidateStateCommandHandler
    : ICommandHandler<ChangeTripParkCandidateStateCommand, ApplicationResult<TripParkCandidateResult>>
{
    private readonly TripProgramService service;

    public ChangeTripParkCandidateStateCommandHandler(TripProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripParkCandidateResult>> HandleAsync(
        ChangeTripParkCandidateStateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.ChangeCandidateStateAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.CandidateId,
            command.ExpectedCandidateVersion,
            command.State,
            cancellationToken);
    }
}
