namespace AmusementPark.WebAPI.Contracts.Trips;

public sealed record TripPilotMetricsDto(
    DateTime GeneratedAtUtc,
    long TotalPlans,
    long CollaborativePlans,
    long PlansWithPreferences,
    long PlansWithDecisions,
    long ExpiredInvitations,
    long EnabledNotificationSubscriptions,
    long AuditEvents,
    long PendingAuditMarkers,
    IReadOnlyDictionary<string, long> ActivityCounts);
