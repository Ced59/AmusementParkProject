using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class PutTripDayPlanCommandHandler
    : ICommandHandler<PutTripDayPlanCommand, ApplicationResult<TripDayPlanResult>>
{
    private readonly TripDayProgramService service;

    public PutTripDayPlanCommandHandler(TripDayProgramService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripDayPlanResult>> HandleAsync(
        PutTripDayPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.PutAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.LocalDate,
            command.ExpectedDayVersion,
            command.Input,
            cancellationToken);
    }
}
