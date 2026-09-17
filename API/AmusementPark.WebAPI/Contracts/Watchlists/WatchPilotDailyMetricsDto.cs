namespace AmusementPark.WebAPI.Contracts.Watchlists;

public sealed class WatchPilotDailyMetricsDto
{
    public string Date { get; init; } = string.Empty;

    public long NotificationsDelivered { get; init; }

    public long DigestsGenerated { get; init; }

    public IReadOnlyDictionary<string, long> InteractionCounts { get; init; } =
        new Dictionary<string, long>(StringComparer.Ordinal);
}
