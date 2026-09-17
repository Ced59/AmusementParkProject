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
    private const int MaximumRangeDays = NotificationDeliveryAttempt.RetentionDays;
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
        DateTime requestedToUtc = NormalizeUtc(query.ToUtc) ?? generatedAtUtc;
        DateTime? requestedFromUtc = NormalizeUtc(query.FromUtc);
        DateTime currentDayUtc = StartOfUtcDay(generatedAtUtc);
        DateTime latestAvailableUtc = EndOfUtcDay(currentDayUtc);
        DateTime earliestAvailableUtc = SubtractWholeDaysOrMinimum(
            currentDayUtc,
            MaximumRangeDays - 1);
        if (requestedToUtc < earliestAvailableUtc
            || requestedToUtc > latestAvailableUtc
            || requestedFromUtc.HasValue
                && (requestedFromUtc.Value < earliestAvailableUtc
                    || requestedFromUtc.Value > requestedToUtc))
        {
            return ApplicationResult<WatchPilotMetricsResult>.Failure(
                WatchPilotApplicationErrors.InvalidMetricsRange());
        }

        DateTime toDayUtc = StartOfUtcDay(requestedToUtc);
        DateTime toUtc = EndOfUtcDay(toDayUtc);
        DateTime defaultFromUtc = SubtractWholeDaysOrMinimum(
            toDayUtc,
            DefaultRangeDays - 1);
        if (defaultFromUtc < earliestAvailableUtc)
        {
            defaultFromUtc = earliestAvailableUtc;
        }

        DateTime fromUtc = requestedFromUtc.HasValue
            ? StartOfUtcDay(requestedFromUtc.Value)
            : defaultFromUtc;

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

    private static DateTime StartOfUtcDay(DateTime value)
    {
        return DateTime.SpecifyKind(value.Date, DateTimeKind.Utc);
    }

    private static DateTime EndOfUtcDay(DateTime startOfDayUtc)
    {
        return startOfDayUtc == DateTime.SpecifyKind(DateTime.MaxValue.Date, DateTimeKind.Utc)
            ? DateTime.SpecifyKind(DateTime.MaxValue, DateTimeKind.Utc)
            : startOfDayUtc.AddDays(1).AddTicks(-1);
    }

    private static DateTime SubtractWholeDaysOrMinimum(DateTime value, int days)
    {
        DateTime minimumUtc = DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        return value < minimumUtc.AddDays(days)
            ? minimumUtc
            : value.AddDays(-days);
    }
}
