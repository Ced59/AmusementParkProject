namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsRenderingSummary
{
    public bool SsrRenderEnabled { get; set; }

    public bool RenderOnCacheMiss { get; set; }

    public bool RenderCriticalRoutesOnCacheMiss { get; set; }

    public int ActiveRenders { get; set; }

    public int QueuedRenders { get; set; }

    public int MaxConcurrency { get; set; }

    public int MaxQueueEntries { get; set; }

    public long TotalRenders { get; set; }

    public long AverageRenderMilliseconds { get; set; }

    public long MaxRenderMilliseconds { get; set; }

    public long SlowRenders { get; set; }

    public long SlowRenderThresholdMilliseconds { get; set; }

    public long QueueFullRejections { get; set; }
}
