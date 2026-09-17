using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class DeleteTripPlanCommandHandler : ICommandHandler<DeleteTripPlanCommand, ApplicationResult>
{
    private readonly TripPlanLifecycleService service;

    public DeleteTripPlanCommandHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteTripPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
