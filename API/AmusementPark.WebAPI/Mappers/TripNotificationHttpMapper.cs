using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripNotificationHttpMapper
{
    public static TripNotificationStateDto ToHttp(this TripNotificationStateResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripNotificationStateDto(
            result.Enabled,
            result.Version,
            result.UnreadCount,
            result.HasMoreUnread);
    }
}
