using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class DeleteWatchSubscriptionCommandHandler
    : ICommandHandler<DeleteWatchSubscriptionCommand, ApplicationResult>
{
    private readonly WatchSubscriptionLifecycleService service;

    public DeleteWatchSubscriptionCommandHandler(WatchSubscriptionLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteWatchSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.DeleteAsync(
            command.UserId,
            command.SubscriptionId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
