using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class DeleteLiveAlertCommandHandler
    : ICommandHandler<DeleteLiveAlertCommand, ApplicationResult>
{
    private readonly LiveAlertLifecycleService service;

    public DeleteLiveAlertCommandHandler(LiveAlertLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        DeleteLiveAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.DeleteAsync(
            command.UserId,
            command.SubscriptionId,
            command.ExpectedVersion,
            cancellationToken);
    }
}
