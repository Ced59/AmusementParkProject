namespace AmusementPark.Core.Domain.Watchlists;

public sealed record WatchPilotHealth(
    decimal DuplicateRatePercent,
    decimal MisleadingReportRatePercent,
    decimal EmailFailureRatePercent,
    decimal AverageDeliveryLatencyMinutes,
    WatchPilotSignal Signal,
    bool CanExtendEventTypes,
    bool ProviderFeedbackAvailable);
