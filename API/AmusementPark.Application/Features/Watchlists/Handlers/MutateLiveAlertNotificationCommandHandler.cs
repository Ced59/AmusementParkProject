using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class MutateLiveAlertNotificationCommandHandler
    : ICommandHandler<MutateLiveAlertNotificationCommand, ApplicationResult>
{
    private readonly LiveAlertLifecycleService service;

    public MutateLiveAlertNotificationCommandHandler(LiveAlertLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        MutateLiveAlertNotificationCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.MutateNotificationAsync(
            command.UserId,
            command.NotificationId,
            command.ExpectedVersion,
            command.Mutation,
            cancellationToken);
    }
}
