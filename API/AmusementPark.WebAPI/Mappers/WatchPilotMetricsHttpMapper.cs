using AmusementPark.Application.Features.Watchlists.Results;
using AmusementPark.WebAPI.Contracts.Watchlists;

namespace AmusementPark.WebAPI.Mappers;

public static class WatchPilotMetricsHttpMapper
{
    public static WatchPilotMetricsDto ToHttp(this WatchPilotMetricsResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new WatchPilotMetricsDto
        {
            GeneratedAtUtc = result.GeneratedAtUtc,
            FromUtc = result.FromUtc,
            ToUtc = result.ToUtc,
            ActiveSubscriptions = result.ActiveSubscriptions,
            ActiveSubscriptionsByEventType = result.ActiveSubscriptionsByEventType,
            EventsVerified = result.EventsVerified,
            EventsPublished = result.EventsPublished,
            EventsCorrected = result.EventsCorrected,
            EventsRetracted = result.EventsRetracted,
            NotificationsDelivered = result.NotificationsDelivered,
            DuplicateNotifications = result.DuplicateNotifications,
            NotificationCenterOpens = result.NotificationCenterOpens,
            SourceOpens = result.SourceOpens,
            MisleadingAlertReports = result.MisleadingAlertReports,
            SubscriptionsRemoved = result.SubscriptionsRemoved,
            DigestsGenerated = result.DigestsGenerated,
            EmailPending = result.EmailPending,
            EmailSucceeded = result.EmailSucceeded,
            EmailFailed = result.EmailFailed,
            EmailCancelled = result.EmailCancelled,
            BounceCount = result.BounceCount,
            ComplaintCount = result.ComplaintCount,
            PendingOutboxEntries = result.PendingOutboxEntries,
            QueueCountsByStatus = result.QueueCountsByStatus,
            Health = new WatchPilotHealthDto
            {
                DuplicateRatePercent = result.Health.DuplicateRatePercent,
                MisleadingReportRatePercent = result.Health.MisleadingReportRatePercent,
                EmailFailureRatePercent = result.Health.EmailFailureRatePercent,
                AverageDeliveryLatencyMinutes = result.Health.AverageDeliveryLatencyMinutes,
                Signal = result.Health.Signal.ToString(),
                CanExtendEventTypes = result.Health.CanExtendEventTypes,
                ProviderFeedbackAvailable = result.Health.ProviderFeedbackAvailable,
            },
            Daily = result.Daily.Select(static day => new WatchPilotDailyMetricsDto
            {
                Date = day.Date,
                NotificationsDelivered = day.NotificationsDelivered,
                DigestsGenerated = day.DigestsGenerated,
                InteractionCounts = day.InteractionCounts,
            }).ToArray(),
        };
    }
}
