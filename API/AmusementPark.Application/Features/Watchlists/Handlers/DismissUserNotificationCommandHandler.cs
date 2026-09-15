using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class DismissUserNotificationCommandHandler
    : ICommandHandler<DismissUserNotificationCommand, ApplicationResult>
{
    private readonly UserNotificationCenterService service;

    public DismissUserNotificationCommandHandler(UserNotificationCenterService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DismissUserNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DismissAsync(
            command.UserId,
            command.NotificationId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
