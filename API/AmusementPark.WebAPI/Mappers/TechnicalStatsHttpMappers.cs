using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.WebAPI.Contracts.TechnicalStats;

namespace AmusementPark.WebAPI.Mappers;

public static class TechnicalStatsHttpMappers
{
    public static TechnicalStatsSnapshotDto ToHttp(this TechnicalStatsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return new TechnicalStatsSnapshotDto
        {
            IsAvailable = snapshot.IsAvailable,
            UnavailableReason = snapshot.UnavailableReason,
            GeneratedAtUtc = snapshot.GeneratedAtUtc,
            StartedAtUtc = snapshot.StartedAtUtc,
            UptimeSeconds = snapshot.UptimeSeconds,
            BuildVersion = snapshot.BuildVersion,
            Daily = snapshot.Daily.Select(ToHttp).ToArray(),
            Cache = snapshot.Cache.ToHttp(),
            Storage = snapshot.Storage.ToHttp(),
            Seo = snapshot.Seo.ToHttp(),
            Rendering = snapshot.Rendering.ToHttp(),
            Refresh = snapshot.Refresh.ToHttp(),
            Invalidation = snapshot.Invalidation.ToHttp(),
            Config = snapshot.Config.ToHttp()
        };
    }

    private static TechnicalStatsDailySnapshotDto ToHttp(TechnicalStatsDailySnapshot snapshot)
    {
        return new TechnicalStatsDailySnapshotDto
        {
            Date = snapshot.Date,
            PageResponses = snapshot.PageResponses,
            CacheHitResponses = snapshot.CacheHitResponses,
            HitRatePercent = snapshot.HitRatePercent,
            RobotPageResponses = snapshot.RobotPageResponses,
            RobotCacheHitResponses = snapshot.RobotCacheHitResponses,
            RobotHitRatePercent = snapshot.RobotHitRatePercent,
            TotalRenders = snapshot.TotalRenders,
            AverageRenderMilliseconds = snapshot.AverageRenderMilliseconds,
            SeoReadyRatePercent = snapshot.SeoReadyRatePercent,
            RobotSeoReadyRatePercent = snapshot.RobotSeoReadyRatePercent,
            RobotCacheOnlyMissResponses = snapshot.RobotCacheOnlyMissResponses,
            QueueFullRejections = snapshot.QueueFullRejections,
            RobotFamilies = snapshot.RobotFamilies.Select(ToHttp).ToArray()
        };
    }

    private static TechnicalStatsCacheSummaryDto ToHttp(this TechnicalStatsCacheSummary summary)
    {
        return new TechnicalStatsCacheSummaryDto
        {
            PageResponses = summary.PageResponses,
            CacheablePageResponses = summary.CacheablePageResponses,
            CacheHitResponses = summary.CacheHitResponses,
            HitRatePercent = summary.HitRatePercent,
            RobotPageResponses = summary.RobotPageResponses,
            RobotCacheHitResponses = summary.RobotCacheHitResponses,
            RobotHitRatePercent = summary.RobotHitRatePercent,
            Statuses = summary.Statuses.Select(static item => item.ToHttp()).ToArray(),
            RobotFamilies = summary.RobotFamilies.Select(static item => item.ToHttp()).ToArray()
        };
    }

    private static TechnicalStatsStorageSummaryDto ToHttp(this TechnicalStatsStorageSummary summary)
    {
        return new TechnicalStatsStorageSummaryDto
        {
            MemoryEntries = summary.MemoryEntries,
            MemoryMaxEntries = summary.MemoryMaxEntries,
            DiskEnabled = summary.DiskEnabled,
            DiskEntries = summary.DiskEntries,
            DiskBytes = summary.DiskBytes,
            DiskMaxBytes = summary.DiskMaxBytes,
            DiskWrites = summary.DiskWrites,
            TechnicalStatsPersistenceEntries = summary.TechnicalStatsPersistenceEntries,
            TechnicalStatsPersistenceBytes = summary.TechnicalStatsPersistenceBytes,
            TechnicalStatsPersistencePurgedBuckets = summary.TechnicalStatsPersistencePurgedBuckets,
            SeoDocumentEntries = summary.SeoDocumentEntries,
            SeoDocumentMaxEntries = summary.SeoDocumentMaxEntries,
            SeoDocumentRequests = summary.SeoDocumentRequests,
            SeoDocumentHits = summary.SeoDocumentHits,
            SeoDocumentMisses = summary.SeoDocumentMisses,
            AssetMisses = summary.AssetMisses
        };
    }

    private static TechnicalStatsSeoSummaryDto ToHttp(this TechnicalStatsSeoSummary summary)
    {
        return new TechnicalStatsSeoSummaryDto
        {
            RobotNoJsHtmlEnabled = summary.RobotNoJsHtmlEnabled,
            HtmlResponses = summary.HtmlResponses,
            SeoReadyHtmlResponses = summary.SeoReadyHtmlResponses,
            SeoNotReadyHtmlResponses = summary.SeoNotReadyHtmlResponses,
            SeoReadyRatePercent = summary.SeoReadyRatePercent,
            RobotHtmlResponses = summary.RobotHtmlResponses,
            RobotSeoReadyHtmlResponses = summary.RobotSeoReadyHtmlResponses,
            RobotSeoNotReadyHtmlResponses = summary.RobotSeoNotReadyHtmlResponses,
            RobotSeoReadyRatePercent = summary.RobotSeoReadyRatePercent,
            RobotNoJsHtmlResponses = summary.RobotNoJsHtmlResponses,
            RobotHtmlBlockedNotSeoReady = summary.RobotHtmlBlockedNotSeoReady,
            RobotHtmlNotAllowed = summary.RobotHtmlNotAllowed,
            RobotSsrUnavailableResponses = summary.RobotSsrUnavailableResponses,
            RobotCacheOnlyMissResponses = summary.RobotCacheOnlyMissResponses,
            RobotPageResponses = summary.RobotPageResponses,
            RobotCacheHitResponses = summary.RobotCacheHitResponses,
            RobotHitRatePercent = summary.RobotHitRatePercent,
            SeoDocumentRequests = summary.SeoDocumentRequests,
            SeoDocumentHits = summary.SeoDocumentHits,
            SeoDocumentMisses = summary.SeoDocumentMisses,
            SeoDocumentHitRatePercent = summary.SeoDocumentHitRatePercent,
            QueueFullRejections = summary.QueueFullRejections
        };
    }

    private static TechnicalStatsRenderingSummaryDto ToHttp(this TechnicalStatsRenderingSummary summary)
    {
        return new TechnicalStatsRenderingSummaryDto
        {
            SsrRenderEnabled = summary.SsrRenderEnabled,
            RenderOnCacheMiss = summary.RenderOnCacheMiss,
            RenderCriticalRoutesOnCacheMiss = summary.RenderCriticalRoutesOnCacheMiss,
            ActiveRenders = summary.ActiveRenders,
            QueuedRenders = summary.QueuedRenders,
            MaxConcurrency = summary.MaxConcurrency,
            MaxQueueEntries = summary.MaxQueueEntries,
            TotalRenders = summary.TotalRenders,
            AverageRenderMilliseconds = summary.AverageRenderMilliseconds,
            MaxRenderMilliseconds = summary.MaxRenderMilliseconds,
            SlowRenders = summary.SlowRenders,
            SlowRenderThresholdMilliseconds = summary.SlowRenderThresholdMilliseconds,
            QueueFullRejections = summary.QueueFullRejections
        };
    }

    private static TechnicalStatsRefreshSummaryDto ToHttp(this TechnicalStatsRefreshSummary summary)
    {
        return new TechnicalStatsRefreshSummaryDto
        {
            Enabled = summary.Enabled,
            PendingRefreshes = summary.PendingRefreshes,
            ActiveRefreshes = summary.ActiveRefreshes,
            DeduplicatedRefreshKeys = summary.DeduplicatedRefreshKeys,
            QueuedRefreshes = summary.QueuedRefreshes,
            SucceededRefreshes = summary.SucceededRefreshes,
            FailedRefreshes = summary.FailedRefreshes,
            MaxUrls = summary.MaxUrls,
            Concurrency = summary.Concurrency,
            DelayMilliseconds = summary.DelayMilliseconds,
            TimeoutSeconds = summary.TimeoutSeconds
        };
    }

    private static TechnicalStatsInvalidationSummaryDto ToHttp(this TechnicalStatsInvalidationSummary summary)
    {
        return new TechnicalStatsInvalidationSummaryDto
        {
            Requests = summary.Requests,
            AllRequests = summary.AllRequests,
            TargetedRequests = summary.TargetedRequests,
            ClearedEntries = summary.ClearedEntries,
            StaleEntries = summary.StaleEntries,
            QueuedRefreshes = summary.QueuedRefreshes,
            LastInvalidationUtc = summary.LastInvalidationUtc
        };
    }

    private static TechnicalStatsRuntimeConfigDto ToHttp(this TechnicalStatsRuntimeConfig config)
    {
        return new TechnicalStatsRuntimeConfigDto
        {
            PageCacheTtlSeconds = config.PageCacheTtlSeconds,
            StalePageCacheSeconds = config.StalePageCacheSeconds,
            PageCacheMaxHtmlBytes = config.PageCacheMaxHtmlBytes,
            PageCacheBrowserCacheControl = config.PageCacheBrowserCacheControl,
            CsrFallbackCacheControl = config.CsrFallbackCacheControl,
            SeoDocumentBrowserCacheControl = config.SeoDocumentBrowserCacheControl,
            TechnicalStatsPersistenceEnabled = config.TechnicalStatsPersistenceEnabled,
            TechnicalStatsPersistenceRetentionDays = config.TechnicalStatsPersistenceRetentionDays,
            TechnicalStatsPersistenceFlushIntervalSeconds = config.TechnicalStatsPersistenceFlushIntervalSeconds,
            TechnicalStatsPersistenceLastFlushUtc = config.TechnicalStatsPersistenceLastFlushUtc,
            TechnicalStatsPersistenceLastCleanupUtc = config.TechnicalStatsPersistenceLastCleanupUtc
        };
    }

    public static TechnicalStatsSettings ToApplication(this UpdateTechnicalStatsSettingsDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);

        return new TechnicalStatsSettings
        {
            PersistenceRetentionDays = dto.PersistenceRetentionDays
        };
    }

    public static TechnicalStatsSettingsDto ToHttp(this TechnicalStatsSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return new TechnicalStatsSettingsDto
        {
            PersistenceRetentionDays = settings.PersistenceRetentionDays
        };
    }

    private static TechnicalStatsCountDto ToHttp(this TechnicalStatsCount count)
    {
        return new TechnicalStatsCountDto
        {
            Key = count.Key,
            Count = count.Count,
            Percent = count.Percent
        };
    }

    private static TechnicalStatsRobotFamilyDto ToHttp(this TechnicalStatsRobotFamily family)
    {
        return new TechnicalStatsRobotFamilyDto
        {
            Key = family.Key,
            Category = family.Category,
            Count = family.Count,
            CacheHits = family.CacheHits,
            HitRatePercent = family.HitRatePercent,
            SeoReadyResponses = family.SeoReadyResponses,
            SeoNotReadyResponses = family.SeoNotReadyResponses,
            SeoReadyRatePercent = family.SeoReadyRatePercent,
            NoJsResponses = family.NoJsResponses,
            BlockedNotSeoReadyResponses = family.BlockedNotSeoReadyResponses,
            HtmlNotAllowedResponses = family.HtmlNotAllowedResponses,
            SsrUnavailableResponses = family.SsrUnavailableResponses
        };
    }
}
