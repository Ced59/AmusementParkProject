using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class DeleteNotificationSourceSubscriptionCommandHandler
    : ICommandHandler<DeleteNotificationSourceSubscriptionCommand, ApplicationResult>
{
    private readonly UserNotificationCenterService service;

    public DeleteNotificationSourceSubscriptionCommandHandler(UserNotificationCenterService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteNotificationSourceSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteSourceSubscriptionAsync(
            command.UserId,
            command.NotificationId,
            command.ExpectedSubscriptionVersion,
            cancellationToken);
    }
}
