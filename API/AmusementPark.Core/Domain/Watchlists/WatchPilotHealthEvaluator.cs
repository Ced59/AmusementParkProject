namespace AmusementPark.Core.Domain.Watchlists;

public static class WatchPilotHealthEvaluator
{
    private const long MinimumNotificationsForExtension = 50;
    private const decimal MaximumDuplicateRatePercent = 0m;
    private const decimal MaximumMisleadingReportRatePercent = 5m;
    private const decimal MaximumEmailFailureRatePercent = 10m;
    private const decimal MaximumAverageLatencyMinutes = 30m;

    public static WatchPilotHealth Evaluate(
        long notificationsDelivered,
        long duplicateNotifications,
        long misleadingReports,
        long emailSucceeded,
        long emailFailed,
        decimal averageDeliveryLatencySeconds,
        bool providerFeedbackAvailable)
    {
        decimal duplicateRate = Percentage(duplicateNotifications, notificationsDelivered);
        decimal misleadingReportRate = Percentage(misleadingReports, notificationsDelivered);
        decimal emailFailureRate = Percentage(emailFailed, emailSucceeded + emailFailed);
        decimal averageLatencyMinutes = Math.Round(
            Math.Max(0m, averageDeliveryLatencySeconds) / 60m,
            1,
            MidpointRounding.AwayFromZero);
        bool qualityAcceptable = duplicateRate <= MaximumDuplicateRatePercent
            && misleadingReportRate < MaximumMisleadingReportRatePercent
            && emailFailureRate < MaximumEmailFailureRatePercent
            && averageLatencyMinutes <= MaximumAverageLatencyMinutes;
        bool canExtend = notificationsDelivered >= MinimumNotificationsForExtension
            && qualityAcceptable
            && providerFeedbackAvailable;
        WatchPilotSignal signal = notificationsDelivered == 0
            ? WatchPilotSignal.AwaitingObservations
            : !qualityAcceptable
                ? WatchPilotSignal.NeedsAttention
                : canExtend
                    ? WatchPilotSignal.ReadyToExtend
                    : WatchPilotSignal.Monitor;

        return new WatchPilotHealth(
            duplicateRate,
            misleadingReportRate,
            emailFailureRate,
            averageLatencyMinutes,
            signal,
            canExtend,
            providerFeedbackAvailable);
    }

    private static decimal Percentage(long numerator, long denominator)
    {
        if (numerator <= 0 || denominator <= 0)
        {
            return 0m;
        }

        return Math.Round(
            Math.Min(100m, numerator * 100m / denominator),
            1,
            MidpointRounding.AwayFromZero);
    }
}
