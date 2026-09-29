using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class CreateLiveAlertCommandHandler
    : ICommandHandler<CreateLiveAlertCommand, ApplicationResult<LiveAlertSubscriptionResult>>
{
    private readonly LiveAlertLifecycleService service;

    public CreateLiveAlertCommandHandler(LiveAlertLifecycleService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<LiveAlertSubscriptionResult>> HandleAsync(
        CreateLiveAlertCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.CreateAsync(command.UserId, command.Input, cancellationToken);
    }
}
