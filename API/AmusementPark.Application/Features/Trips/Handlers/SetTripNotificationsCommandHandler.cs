using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class SetTripNotificationsCommandHandler
    : ICommandHandler<SetTripNotificationsCommand, ApplicationResult<TripNotificationStateResult>>
{
    private readonly TripNotificationService service;

    public SetTripNotificationsCommandHandler(TripNotificationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripNotificationStateResult>> HandleAsync(
        SetTripNotificationsCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetEnabledAsync(
            command.UserId,
            command.TripPlanId,
            command.Enabled,
            command.ExpectedVersion,
            cancellationToken);
    }
}
