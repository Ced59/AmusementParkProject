using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class DeleteTripParkCandidateCommandHandler
    : ICommandHandler<DeleteTripParkCandidateCommand, ApplicationResult>
{
    private readonly TripProgramService service;

    public DeleteTripParkCandidateCommandHandler(TripProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteTripParkCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteCandidateAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.CandidateId,
            command.ExpectedCandidateVersion,
            cancellationToken);
    }
}
