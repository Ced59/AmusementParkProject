using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Commands;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class MarkTripNotificationsReadCommandHandler
    : ICommandHandler<MarkTripNotificationsReadCommand, ApplicationResult<TripNotificationStateResult>>
{
    private readonly TripNotificationService service;

    public MarkTripNotificationsReadCommandHandler(TripNotificationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<TripNotificationStateResult>> HandleAsync(
        MarkTripNotificationsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.MarkReadAsync(
            command.UserId,
            command.TripPlanId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
