using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class AddTripParkCandidateCommandHandler
    : ICommandHandler<AddTripParkCandidateCommand, ApplicationResult<CreateTripParkCandidateResult>>
{
    private readonly TripProgramService service;

    public AddTripParkCandidateCommandHandler(TripProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<CreateTripParkCandidateResult>> HandleAsync(
        AddTripParkCandidateCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.AddCandidateAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.IdempotencyKey,
            command.Input,
            cancellationToken);
    }
}
