using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Services;

public sealed class UserNotificationCreationService
{
    private readonly IUserNotificationRepository notificationRepository;
    private readonly WatchPilotMetricsRecorder metricsRecorder;

    public UserNotificationCreationService(
        IUserNotificationRepository notificationRepository,
        WatchPilotMetricsRecorder metricsRecorder)
    {
        this.notificationRepository = notificationRepository
            ?? throw new ArgumentNullException(nameof(notificationRepository));
        this.metricsRecorder = metricsRecorder
            ?? throw new ArgumentNullException(nameof(metricsRecorder));
    }

    public async Task<UserNotificationCreationResult> CreateManyAsync(
        IReadOnlyCollection<UserNotification> notifications,
        CancellationToken cancellationToken)
    {
        UserNotificationCreationResult result = await this.notificationRepository.CreateManyAsync(
            notifications,
            cancellationToken);
        if (result.DuplicateCount > 0)
        {
            await this.metricsRecorder.RecordCountBestEffortAsync(
                WatchPilotInteractionKind.DuplicateDeliveryPrevented,
                result.DuplicateCount,
                cancellationToken);
        }

        return result;
    }
}
