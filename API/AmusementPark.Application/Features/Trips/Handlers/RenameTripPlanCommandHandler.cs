using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class RenameTripPlanCommandHandler
    : ICommandHandler<RenameTripPlanCommand, ApplicationResult<TripPlanResult>>
{
    private readonly TripPlanLifecycleService service;

    public RenameTripPlanCommandHandler(TripPlanLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<TripPlanResult>> HandleAsync(
        RenameTripPlanCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.RenameAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedVersion,
            command.Title,
            cancellationToken);
    }
}
