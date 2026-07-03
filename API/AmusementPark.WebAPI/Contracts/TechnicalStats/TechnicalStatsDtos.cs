namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

public sealed class TechnicalStatsSnapshotDto
{
    public bool IsAvailable { get; set; } = true;

    public string? UnavailableReason { get; set; }

    public DateTime GeneratedAtUtc { get; set; }

    public DateTime StartedAtUtc { get; set; }

    public long UptimeSeconds { get; set; }

    public string BuildVersion { get; set; } = string.Empty;

    public TechnicalStatsCacheSummaryDto Cache { get; set; } = new TechnicalStatsCacheSummaryDto();

    public TechnicalStatsStorageSummaryDto Storage { get; set; } = new TechnicalStatsStorageSummaryDto();

    public TechnicalStatsSeoSummaryDto Seo { get; set; } = new TechnicalStatsSeoSummaryDto();

    public TechnicalStatsRenderingSummaryDto Rendering { get; set; } = new TechnicalStatsRenderingSummaryDto();

    public TechnicalStatsRefreshSummaryDto Refresh { get; set; } = new TechnicalStatsRefreshSummaryDto();

    public TechnicalStatsInvalidationSummaryDto Invalidation { get; set; } = new TechnicalStatsInvalidationSummaryDto();

    public TechnicalStatsRuntimeConfigDto Config { get; set; } = new TechnicalStatsRuntimeConfigDto();
}

public sealed class TechnicalStatsCacheSummaryDto
{
    public long PageResponses { get; set; }

    public long CacheablePageResponses { get; set; }

    public long CacheHitResponses { get; set; }

    public double HitRatePercent { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public IReadOnlyCollection<TechnicalStatsCountDto> Statuses { get; set; } = Array.Empty<TechnicalStatsCountDto>();

    public IReadOnlyCollection<TechnicalStatsRobotFamilyDto> RobotFamilies { get; set; } = Array.Empty<TechnicalStatsRobotFamilyDto>();
}

public sealed class TechnicalStatsStorageSummaryDto
{
    public int MemoryEntries { get; set; }

    public int MemoryMaxEntries { get; set; }

    public bool DiskEnabled { get; set; }

    public int DiskEntries { get; set; }

    public long DiskBytes { get; set; }

    public long DiskMaxBytes { get; set; }

    public long DiskWrites { get; set; }

    public int TechnicalStatsPersistenceEntries { get; set; }

    public long TechnicalStatsPersistenceBytes { get; set; }

    public int TechnicalStatsPersistencePurgedBuckets { get; set; }

    public int SeoDocumentEntries { get; set; }

    public int SeoDocumentMaxEntries { get; set; }

    public long SeoDocumentRequests { get; set; }

    public long SeoDocumentHits { get; set; }

    public long SeoDocumentMisses { get; set; }

    public long AssetMisses { get; set; }
}

public sealed class TechnicalStatsSeoSummaryDto
{
    public bool RobotNoJsHtmlEnabled { get; set; }

    public long HtmlResponses { get; set; }

    public long SeoReadyHtmlResponses { get; set; }

    public long SeoNotReadyHtmlResponses { get; set; }

    public double SeoReadyRatePercent { get; set; }

    public long RobotHtmlResponses { get; set; }

    public long RobotSeoReadyHtmlResponses { get; set; }

    public long RobotSeoNotReadyHtmlResponses { get; set; }

    public double RobotSeoReadyRatePercent { get; set; }

    public long RobotNoJsHtmlResponses { get; set; }

    public long RobotHtmlBlockedNotSeoReady { get; set; }

    public long RobotHtmlNotAllowed { get; set; }

    public long RobotSsrUnavailableResponses { get; set; }

    public long RobotPageResponses { get; set; }

    public long RobotCacheHitResponses { get; set; }

    public double RobotHitRatePercent { get; set; }

    public long SeoDocumentRequests { get; set; }

    public long SeoDocumentHits { get; set; }

    public long SeoDocumentMisses { get; set; }

    public double SeoDocumentHitRatePercent { get; set; }

    public long QueueFullRejections { get; set; }
}

public sealed class TechnicalStatsRenderingSummaryDto
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

public sealed class TechnicalStatsRuntimeConfigDto
{
    public long PageCacheTtlSeconds { get; set; }

    public long StalePageCacheSeconds { get; set; }

    public long PageCacheMaxHtmlBytes { get; set; }

    public string PageCacheBrowserCacheControl { get; set; } = string.Empty;

    public string CsrFallbackCacheControl { get; set; } = string.Empty;

    public string SeoDocumentBrowserCacheControl { get; set; } = string.Empty;

    public bool TechnicalStatsPersistenceEnabled { get; set; }

    public int TechnicalStatsPersistenceRetentionDays { get; set; }

    public int TechnicalStatsPersistenceFlushIntervalSeconds { get; set; }

    public DateTime? TechnicalStatsPersistenceLastFlushUtc { get; set; }

    public DateTime? TechnicalStatsPersistenceLastCleanupUtc { get; set; }
}

public sealed class UpdateTechnicalStatsSettingsDto
{
    public int PersistenceRetentionDays { get; set; }
}

public sealed class TechnicalStatsSettingsDto
{
    public int PersistenceRetentionDays { get; set; }
}

public sealed class TechnicalStatsCountDto
{
    public string Key { get; set; } = string.Empty;

    public long Count { get; set; }

    public double Percent { get; set; }
}

public sealed class TechnicalStatsRobotFamilyDto
{
    public string Key { get; set; } = string.Empty;

    public string Category { get; set; } = string.Empty;

    public long Count { get; set; }

    public long CacheHits { get; set; }

    public double HitRatePercent { get; set; }

    public long SeoReadyResponses { get; set; }

    public long SeoNotReadyResponses { get; set; }

    public double SeoReadyRatePercent { get; set; }

    public long NoJsResponses { get; set; }

    public long BlockedNotSeoReadyResponses { get; set; }

    public long HtmlNotAllowedResponses { get; set; }

    public long SsrUnavailableResponses { get; set; }
}
