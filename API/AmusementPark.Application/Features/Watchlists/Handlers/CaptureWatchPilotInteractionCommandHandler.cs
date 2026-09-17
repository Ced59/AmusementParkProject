using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Commands;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Identifiers;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class CaptureWatchPilotInteractionCommandHandler
    : ICommandHandler<CaptureWatchPilotInteractionCommand, ApplicationResult>
{
    private readonly IUserNotificationRepository notificationRepository;
    private readonly WatchPilotMetricsRecorder metricsRecorder;
    private readonly TimeProvider timeProvider;

    public CaptureWatchPilotInteractionCommandHandler(
        IUserNotificationRepository notificationRepository,
        WatchPilotMetricsRecorder metricsRecorder,
        TimeProvider? timeProvider = null)
    {
        this.notificationRepository = notificationRepository ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.metricsRecorder = metricsRecorder ?? throw new ArgumentNullException(nameof(metricsRecorder));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult> HandleAsync(
        CaptureWatchPilotInteractionCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        string normalizedUserId;
        try
        {
            normalizedUserId = IdentifierRules.NormalizeRequired(command.UserId, nameof(command.UserId));
        }
        catch (ArgumentException)
        {
            return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
        }

        if (command.InteractionKind is not (
            WatchPilotInteractionKind.NotificationCenterOpened
            or WatchPilotInteractionKind.SourceOpened
            or WatchPilotInteractionKind.MisleadingAlertReported))
        {
            return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
        }

        bool requiresNotification = command.InteractionKind is WatchPilotInteractionKind.SourceOpened
            or WatchPilotInteractionKind.MisleadingAlertReported;
        UserNotification? notification = null;
        if (requiresNotification)
        {
            if (!UserNotificationId.TryParse(command.NotificationId, out UserNotificationId notificationId)
                || (notification = await this.notificationRepository.GetOwnedAsync(
                    normalizedUserId,
                    notificationId,
                    cancellationToken)) is null)
            {
                return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
            }
        }
        else if (!string.IsNullOrWhiteSpace(command.NotificationId))
        {
            return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
        }

        if (command.InteractionKind == WatchPilotInteractionKind.MisleadingAlertReported)
        {
            return await this.ReportMisleadingAsync(
                normalizedUserId,
                notification!,
                cancellationToken);
        }

        await this.metricsRecorder.RecordBestEffortAsync(
            command.InteractionKind,
            cancellationToken);
        return ApplicationResult.Success();
    }

    private async Task<ApplicationResult> ReportMisleadingAsync(
        string userId,
        UserNotification notification,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        for (int attempt = 0; attempt < 2; attempt++)
        {
            if (notification.MisleadingReportedAtUtc.HasValue)
            {
                return ApplicationResult.Success();
            }

            long expectedVersion = notification.Version;
            try
            {
                notification.ReportMisleading(nowUtc);
            }
            catch (UserNotificationValidationException)
            {
                return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
            }

            UserNotificationWriteOutcome outcome = await this.notificationRepository.ReplaceAsync(
                notification,
                expectedVersion,
                cancellationToken);
            if (outcome == UserNotificationWriteOutcome.Success)
            {
                await this.metricsRecorder.RecordBestEffortAsync(
                    WatchPilotInteractionKind.MisleadingAlertReported,
                    cancellationToken);
                return ApplicationResult.Success();
            }

            UserNotification? current = await this.notificationRepository.GetOwnedAsync(
                userId,
                notification.Id,
                cancellationToken);
            if (current is null)
            {
                return ApplicationResult.Failure(WatchPilotApplicationErrors.InvalidInteraction());
            }

            notification = current;
        }

        return ApplicationResult.Failure(WatchPilotApplicationErrors.ChangedConcurrently());
    }
}
