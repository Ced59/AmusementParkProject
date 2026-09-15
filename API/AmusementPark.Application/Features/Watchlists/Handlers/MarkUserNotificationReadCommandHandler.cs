using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class MarkUserNotificationReadCommandHandler
    : ICommandHandler<MarkUserNotificationReadCommand, ApplicationResult>
{
    private readonly UserNotificationCenterService service;

    public MarkUserNotificationReadCommandHandler(UserNotificationCenterService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        MarkUserNotificationReadCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.MarkReadAsync(
            command.UserId,
            command.NotificationId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
