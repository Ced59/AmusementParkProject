namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsInvalidationSummary
{
    public long Requests { get; set; }

    public long AllRequests { get; set; }

    public long TargetedRequests { get; set; }

    public long ClearedEntries { get; set; }

    public long StaleEntries { get; set; }

    public long QueuedRefreshes { get; set; }

    public DateTime? LastInvalidationUtc { get; set; }
}
