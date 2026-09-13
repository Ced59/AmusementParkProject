namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsInvalidationSummaryDto
{
    public long Requests { get; set; }

    public long AllRequests { get; set; }

    public long TargetedRequests { get; set; }

    public long ClearedEntries { get; set; }

    public long StaleEntries { get; set; }

    public long QueuedRefreshes { get; set; }

    public DateTime? LastInvalidationUtc { get; set; }
}
