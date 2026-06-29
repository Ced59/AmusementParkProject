using AmusementPark.Application.Features.ParkOpeningHours.Ports;

namespace AmusementPark.Application.Features.ParkOpeningHours.Services;

public sealed class NoOpParkOpeningHoursNotificationService : IParkOpeningHoursNotificationService
{
    public Task NotifyCoverageThresholdReachedAsync(ParkOpeningHoursCoverageNotification notification, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }
}
