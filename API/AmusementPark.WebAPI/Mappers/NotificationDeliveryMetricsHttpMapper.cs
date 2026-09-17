using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

internal static class NotificationDeliveryMetricsHttpMapper
{
    internal static NotificationDeliveryMetricsDto ToHttp(
        this NotificationDeliveryMetricsResult result)
    {
        return new NotificationDeliveryMetricsDto(
            result.FromUtc,
            result.ToUtc,
            result.PendingCount,
            result.FailedCount,
            result.SucceededCount,
            result.CancelledCount,
            result.LatestFailureAtUtc);
    }
}
