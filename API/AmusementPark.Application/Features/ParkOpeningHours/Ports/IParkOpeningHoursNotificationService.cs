namespace AmusementPark.Application.Features.ParkOpeningHours.Ports;

public interface IParkOpeningHoursNotificationService
{
    Task NotifyCoverageThresholdReachedAsync(ParkOpeningHoursCoverageNotification notification, CancellationToken cancellationToken);
}
