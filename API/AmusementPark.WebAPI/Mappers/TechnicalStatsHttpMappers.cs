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
            Cache = snapshot.Cache.ToHttp(),
            Storage = snapshot.Storage.ToHttp(),
            Rendering = snapshot.Rendering.ToHttp(),
            Refresh = snapshot.Refresh.ToHttp(),
            Invalidation = snapshot.Invalidation.ToHttp(),
            Config = snapshot.Config.ToHttp()
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
            SeoDocumentEntries = summary.SeoDocumentEntries,
            SeoDocumentMaxEntries = summary.SeoDocumentMaxEntries,
            SeoDocumentRequests = summary.SeoDocumentRequests,
            SeoDocumentHits = summary.SeoDocumentHits,
            SeoDocumentMisses = summary.SeoDocumentMisses,
            AssetMisses = summary.AssetMisses
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
            SeoDocumentBrowserCacheControl = config.SeoDocumentBrowserCacheControl
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
            Count = family.Count,
            CacheHits = family.CacheHits,
            HitRatePercent = family.HitRatePercent
        };
    }
}
