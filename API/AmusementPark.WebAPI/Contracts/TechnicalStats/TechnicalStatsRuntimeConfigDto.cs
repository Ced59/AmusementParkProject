namespace AmusementPark.WebAPI.Contracts.TechnicalStats;

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
