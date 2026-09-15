using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class SetWatchSubscriptionPausedCommandHandler
    : ICommandHandler<SetWatchSubscriptionPausedCommand, ApplicationResult<WatchSubscriptionResult>>
{
    private readonly WatchSubscriptionLifecycleService service;

    public SetWatchSubscriptionPausedCommandHandler(WatchSubscriptionLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<WatchSubscriptionResult>> HandleAsync(
        SetWatchSubscriptionPausedCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.SetPausedAsync(
            command.UserId,
            command.SubscriptionId,
            command.ExpectedVersion,
            command.Paused,
            cancellationToken);
    }
}
