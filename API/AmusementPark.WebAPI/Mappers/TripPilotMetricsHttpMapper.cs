using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.WebAPI.Contracts.Trips;

namespace AmusementPark.WebAPI.Mappers;

public static class TripPilotMetricsHttpMapper
{
    public static TripPilotMetricsDto ToHttp(this TripPilotMetricsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new TripPilotMetricsDto(
            result.GeneratedAtUtc,
            result.TotalPlans,
            result.CollaborativePlans,
            result.PlansWithPreferences,
            result.PlansWithDecisions,
            result.ExpiredInvitations,
            result.EnabledNotificationSubscriptions,
            result.AuditEvents,
            result.PendingAuditMarkers,
            result.ActivityCounts);
    }
}
