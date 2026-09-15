using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.ParkOpeningHours.Models;

namespace AmusementPark.Application.Features.ParkOpeningHours.Ports;

public interface IParkOpeningHoursFactualChangeCapture
{
    ParkOpeningHoursFactualChangeDraft? Prepare(
        Park park,
        ParkOpeningHoursSchedule? previousSchedule,
        ParkOpeningHoursSchedule currentSchedule);

    Task CaptureAsync(
        ParkOpeningHoursPendingFactualChange pendingFactualChange,
        CancellationToken cancellationToken);

    Task<int> ReconcilePendingAsync(
        int maximumCount,
        CancellationToken cancellationToken);
}
