using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class MoveTripParkCandidateCommandHandler
    : ICommandHandler<MoveTripParkCandidateCommand, ApplicationResult<TripProgramResult>>
{
    private readonly TripProgramService service;

    public MoveTripParkCandidateCommandHandler(TripProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripProgramResult>> HandleAsync(
        MoveTripParkCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.MoveCandidateAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.CandidateId,
            command.AnchorCandidateId,
            command.Placement,
            cancellationToken);
    }
}
