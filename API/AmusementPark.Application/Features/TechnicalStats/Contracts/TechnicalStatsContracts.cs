namespace AmusementPark.Application.Features.TechnicalStats.Contracts;

public sealed class TechnicalStatsSnapshot
{
    public bool IsAvailable { get; set; } = true;

    public string? UnavailableReason { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public long UptimeSeconds { get; set; }

    public string BuildVersion { get; set; } = string.Empty;

    public TechnicalStatsCacheSummary Cache { get; set; } = new TechnicalStatsCacheSummary();

    public TechnicalStatsStorageSummary Storage { get; set; } = new TechnicalStatsStorageSummary();

    public TechnicalStatsRenderingSummary Rendering { get; set; } = new TechnicalStatsRenderingSummary();

    public TechnicalStatsRefreshSummary Refresh { get; set; } = new TechnicalStatsRefreshSummary();

    public TechnicalStatsInvalidationSummary Invalidation { get; set; } = new TechnicalStatsInvalidationSummary();

    public TechnicalStatsRuntimeConfig Config { get; set; } = new TechnicalStatsRuntimeConfig();
}

public sealed class TechnicalStatsCacheSummary
{
    public long PageResponses { get; set; }

    public long CacheablePageResponses { get; set; }

    public long CacheHitResponses { get; set; }

    public double HitRatePercent { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public IReadOnlyCollection<TechnicalStatsCount> Statuses { get; set; } = Array.Empty<TechnicalStatsCount>();

    public IReadOnlyCollection<TechnicalStatsRobotFamily> RobotFamilies { get; set; } = Array.Empty<TechnicalStatsRobotFamily>();
}

public sealed class TechnicalStatsStorageSummary
{
    public int MemoryEntries { get; set; }

    public int MemoryMaxEntries { get; set; }

    public bool DiskEnabled { get; set; }

    public int DiskEntries { get; set; }

    public long DiskBytes { get; set; }

    public long DiskMaxBytes { get; set; }

    public long DiskWrites { get; set; }

    public int SeoDocumentEntries { get; set; }

    public int SeoDocumentMaxEntries { get; set; }

    public long SeoDocumentRequests { get; set; }

    public long SeoDocumentHits { get; set; }

    public long SeoDocumentMisses { get; set; }

    public long AssetMisses { get; set; }
}

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

public sealed class TechnicalStatsRefreshSummary
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

public sealed class TechnicalStatsRuntimeConfig
{
    public long PageCacheTtlSeconds { get; set; }

    public long StalePageCacheSeconds { get; set; }

    public long PageCacheMaxHtmlBytes { get; set; }

    public string PageCacheBrowserCacheControl { get; set; } = string.Empty;

    public string CsrFallbackCacheControl { get; set; } = string.Empty;

    public string SeoDocumentBrowserCacheControl { get; set; } = string.Empty;
}

public sealed class TechnicalStatsCount
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }

    public double Percent { get; set; }
}

public sealed class TechnicalStatsRobotFamily
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }

    public long CacheHits { get; set; }

    public double HitRatePercent { get; set; }
}
