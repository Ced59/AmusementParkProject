using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class SetTripPlanDatesCommandHandler
    : ICommandHandler<SetTripPlanDatesCommand, ApplicationResult<TripPlanResult>>
{
    private readonly TripPlanLifecycleService service;

    public SetTripPlanDatesCommandHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPlanResult>> HandleAsync(
        SetTripPlanDatesCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetDatesAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedVersion,
            command.Input,
            cancellationToken);
    }
}
