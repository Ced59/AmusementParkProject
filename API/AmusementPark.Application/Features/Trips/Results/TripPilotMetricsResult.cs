namespace AmusementPark.Application.Features.Trips.Results;

public sealed record TripPilotMetricsResult(
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
