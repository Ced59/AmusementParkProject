namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchPilotHealthDto
{
    public decimal DuplicateRatePercent { get; init; }

    public decimal MisleadingReportRatePercent { get; init; }

    public decimal EmailFailureRatePercent { get; init; }

    public decimal AverageDeliveryLatencyMinutes { get; init; }

    public string Signal { get; init; } = string.Empty;

    public bool CanExtendEventTypes { get; init; }

    public bool ProviderFeedbackAvailable { get; init; }
}
