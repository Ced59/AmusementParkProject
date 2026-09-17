using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class DeleteTripDayPlanCommandHandler
    : ICommandHandler<DeleteTripDayPlanCommand, ApplicationResult>
{
    private readonly TripDayProgramService service;

    public DeleteTripDayPlanCommandHandler(TripDayProgramService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteTripDayPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedPlanVersion,
            command.LocalDate,
            command.ExpectedDayVersion,
            cancellationToken);
    }
}
