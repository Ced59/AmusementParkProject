using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class MarkAllUserNotificationsReadCommandHandler
    : ICommandHandler<MarkAllUserNotificationsReadCommand, ApplicationResult>
{
    private readonly UserNotificationCenterService service;

    public MarkAllUserNotificationsReadCommandHandler(UserNotificationCenterService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        MarkAllUserNotificationsReadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.MarkAllReadAsync(command.UserId, cancellationToken);
    }
}
