using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class CreateWatchSubscriptionCommandHandler
    : ICommandHandler<CreateWatchSubscriptionCommand, ApplicationResult<WatchSubscriptionResult>>
{
    private readonly WatchSubscriptionLifecycleService service;

    public CreateWatchSubscriptionCommandHandler(WatchSubscriptionLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<WatchSubscriptionResult>> HandleAsync(
        CreateWatchSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.CreateAsync(command.UserId, command.Input, cancellationToken);
    }
}
