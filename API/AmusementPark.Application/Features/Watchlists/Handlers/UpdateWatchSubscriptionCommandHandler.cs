using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class UpdateWatchSubscriptionCommandHandler
    : ICommandHandler<UpdateWatchSubscriptionCommand, ApplicationResult<WatchSubscriptionResult>>
{
    private readonly WatchSubscriptionLifecycleService service;

    public UpdateWatchSubscriptionCommandHandler(WatchSubscriptionLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<WatchSubscriptionResult>> HandleAsync(
        UpdateWatchSubscriptionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.UpdateAsync(
            command.UserId,
            command.SubscriptionId,
            command.ExpectedVersion,
            command.Input,
            cancellationToken);
    }
}
