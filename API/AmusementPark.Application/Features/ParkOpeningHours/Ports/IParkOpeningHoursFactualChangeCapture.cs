using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkOpeningHours.Ports;

public interface IParkOpeningHoursFactualChangeCapture
{
    Task CaptureAsync(
        Park park,
        ParkOpeningHoursSchedule? previousSchedule,
        ParkOpeningHoursSchedule currentSchedule,
        CancellationToken cancellationToken);
}
