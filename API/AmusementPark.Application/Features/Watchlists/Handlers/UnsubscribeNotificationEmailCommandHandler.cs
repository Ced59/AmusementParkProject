using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class UnsubscribeNotificationEmailCommandHandler
    : ICommandHandler<UnsubscribeNotificationEmailCommand, ApplicationResult>
{
    private readonly INotificationEmailUnsubscribeTokenProtector tokenProtector;
    private readonly NotificationEmailPreferenceService service;

    public UnsubscribeNotificationEmailCommandHandler(
        INotificationEmailUnsubscribeTokenProtector tokenProtector,
        NotificationEmailPreferenceService service)
    {
        this.tokenProtector = tokenProtector;
        this.service = service;
    }

    public Task<ApplicationResult> HandleAsync(
        UnsubscribeNotificationEmailCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        if (!this.tokenProtector.TryReadUserId(command.Token, out string userId))
        {
            return Task.FromResult(
                ApplicationResult.Failure(NotificationEmailPreferenceApplicationErrors.Invalid()));
        }

        return this.service.RevokeByTokenAsync(userId, cancellationToken);
    }
}
