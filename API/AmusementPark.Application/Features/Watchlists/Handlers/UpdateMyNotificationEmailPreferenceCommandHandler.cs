using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class UpdateMyNotificationEmailPreferenceCommandHandler
    : ICommandHandler<UpdateMyNotificationEmailPreferenceCommand,
        ApplicationResult<NotificationEmailPreferenceResult>>
{
    private readonly NotificationEmailPreferenceService service;

    public UpdateMyNotificationEmailPreferenceCommandHandler(NotificationEmailPreferenceService service)
    {
        this.service = service;
    }

    public Task<ApplicationResult<NotificationEmailPreferenceResult>> HandleAsync(
        UpdateMyNotificationEmailPreferenceCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.service.UpdateAsync(command.UserId, command.Input, cancellationToken);
    }
}
