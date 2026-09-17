using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class CreateTripPlanCommandHandler
    : ICommandHandler<CreateTripPlanCommand, ApplicationResult<CreateTripPlanResult>>
{
    private readonly TripPlanLifecycleService service;

    public CreateTripPlanCommandHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<CreateTripPlanResult>> HandleAsync(
        CreateTripPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.CreateAsync(
            command.UserId,
            command.ClientOperationId,
            command.Input,
            cancellationToken);
    }
}
