using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Queries;
using AmusementPark.Application.Features.Trips.Results;

namespace AmusementPark.Application.Features.Trips.Handlers;

public sealed class GetTripPilotMetricsQueryHandler
    : IQueryHandler<GetTripPilotMetricsQuery, ApplicationResult<TripPilotMetricsResult>>
{
    private readonly ITripPilotMetricsRepository repository;
    private readonly TimeProvider timeProvider;

    public GetTripPilotMetricsQueryHandler(
        ITripPilotMetricsRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<TripPilotMetricsResult>> HandleAsync(
        GetTripPilotMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        TripPilotMetricsSnapshot snapshot = await this.repository.ReadAsync(cancellationToken);
        return ApplicationResult<TripPilotMetricsResult>.Success(new TripPilotMetricsResult(
            this.timeProvider.GetUtcNow().UtcDateTime,
            snapshot.TotalPlans,
            snapshot.CollaborativePlans,
            snapshot.PlansWithPreferences,
            snapshot.PlansWithDecisions,
            snapshot.ExpiredInvitations,
            snapshot.EnabledNotificationSubscriptions,
            snapshot.AuditEvents,
            snapshot.PendingAuditMarkers,
            snapshot.ActivityCounts));
    }
}
