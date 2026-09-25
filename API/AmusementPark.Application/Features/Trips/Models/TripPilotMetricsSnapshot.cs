namespace AmusementPark.Application.Features.Trips.Models;

public sealed record TripPilotMetricsSnapshot(
    long TotalPlans,
    long CollaborativePlans,
    long PlansWithPreferences,
    long PlansWithDecisions,
    long ExpiredInvitations,
    long EnabledNotificationSubscriptions,
    long AuditEvents,
    long PendingAuditMarkers,
    IReadOnlyDictionary<string, long> ActivityCounts);
