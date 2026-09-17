using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Ports;
using AmusementPark.Application.Features.Watchlists.Queries;
using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.Core.Domain.Watchlists;

namespace AmusementPark.Application.Features.Watchlists.Handlers;

public sealed class GetWatchPilotMetricsQueryHandler
    : IQueryHandler<GetWatchPilotMetricsQuery, ApplicationResult<WatchPilotMetricsResult>>
{
    private const int DefaultRangeDays = 30;
    private const int MaximumRangeDays = 180;
    private readonly IWatchPilotMetricsRepository repository;
    private readonly TimeProvider timeProvider;

    public GetWatchPilotMetricsQueryHandler(
        IWatchPilotMetricsRepository repository,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<WatchPilotMetricsResult>> HandleAsync(
        GetWatchPilotMetricsQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(query);
        DateTime generatedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        DateTime toUtc = NormalizeUtc(query.ToUtc) ?? generatedAtUtc;
        DateTime? requestedFromUtc = NormalizeUtc(query.FromUtc);
        if (requestedFromUtc.HasValue && requestedFromUtc.Value > toUtc)
        {
            return ApplicationResult<WatchPilotMetricsResult>.Failure(
                WatchPilotApplicationErrors.InvalidMetricsRange());
        }

        DateTime fromUtc = requestedFromUtc ?? toUtc.AddDays(-(DefaultRangeDays - 1));
        DateTime earliestUtc = toUtc.AddDays(-(MaximumRangeDays - 1));
        if (fromUtc < earliestUtc)
        {
            fromUtc = earliestUtc;
        }

        WatchPilotMetricsSnapshot snapshot = await this.repository.ReadAsync(
            fromUtc,
            toUtc,
            cancellationToken);
        long centerOpens = Sum(snapshot.Daily, WatchPilotInteractionKind.NotificationCenterOpened);
        long sourceOpens = Sum(snapshot.Daily, WatchPilotInteractionKind.SourceOpened);
        long misleadingReports = Sum(snapshot.Daily, WatchPilotInteractionKind.MisleadingAlertReported);
        long subscriptionsRemoved = Sum(snapshot.Daily, WatchPilotInteractionKind.SubscriptionRemoved);
        WatchPilotHealth health = WatchPilotHealthEvaluator.Evaluate(
            snapshot.NotificationsDelivered,
            snapshot.DuplicateNotifications,
            misleadingReports,
            snapshot.EmailSucceeded,
            snapshot.EmailFailed,
            snapshot.AverageDeliveryLatencySeconds,
            snapshot.ProviderFeedbackAvailable);
        WatchPilotMetricsResult result = new(
            generatedAtUtc,
            fromUtc,
            toUtc,
            snapshot.ActiveSubscriptions,
            snapshot.ActiveSubscriptionsByEventType,
            snapshot.EventsVerified,
            snapshot.EventsPublished,
            snapshot.EventsCorrected,
            snapshot.EventsRetracted,
            snapshot.NotificationsDelivered,
            snapshot.DuplicateNotifications,
            centerOpens,
            sourceOpens,
            misleadingReports,
            subscriptionsRemoved,
            snapshot.DigestsGenerated,
            snapshot.EmailPending,
            snapshot.EmailSucceeded,
            snapshot.EmailFailed,
            snapshot.EmailCancelled,
            snapshot.BounceCount,
            snapshot.ComplaintCount,
            snapshot.PendingOutboxEntries,
            snapshot.QueueCountsByStatus,
            health,
            snapshot.Daily);
        return ApplicationResult<WatchPilotMetricsResult>.Success(result);
    }

    private static long Sum(
        IEnumerable<WatchPilotDailyMetrics> daily,
        WatchPilotInteractionKind kind)
    {
        return daily.Sum(day => day.InteractionCounts.GetValueOrDefault(kind.ToString()));
    }

    private static DateTime? NormalizeUtc(DateTime? value)
    {
        if (!value.HasValue)
        {
            return null;
        }

        return value.Value.Kind == DateTimeKind.Utc
            ? value.Value
            : value.Value.ToUniversalTime();
    }
}
