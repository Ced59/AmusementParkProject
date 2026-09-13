namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsRefreshSummaryDto
{
    public bool Enabled { get; set; }

    public int PendingRefreshes { get; set; }

    public int ActiveRefreshes { get; set; }

    public int DeduplicatedRefreshKeys { get; set; }

    public long QueuedRefreshes { get; set; }

    public long SucceededRefreshes { get; set; }

    public long FailedRefreshes { get; set; }

    public int MaxUrls { get; set; }

    public int Concurrency { get; set; }

    public long DelayMilliseconds { get; set; }

    public long TimeoutSeconds { get; set; }
}
