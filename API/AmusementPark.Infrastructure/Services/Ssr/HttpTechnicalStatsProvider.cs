using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using AmusementPark.Application.Features.TechnicalStats.Contracts;
using AmusementPark.Application.Features.TechnicalStats.Ports;
using AmusementPark.Infrastructure.Configuration.Ssr;
using Microsoft.Extensions.Logging;

namespace AmusementPark.Infrastructure.Services.Ssr;

public sealed class HttpTechnicalStatsProvider : ITechnicalStatsProvider
{
    public const string HttpClientName = "ssr-technical-stats";

    private const string TokenHeaderName = "X-AmusementPark-Cache-Token";
    private const string TechnicalStatsPath = "/internal/technical-stats";
    private const string TechnicalStatsSettingsPath = "/internal/technical-stats/settings";
    private const int MaxDistributionRows = 50;

    private readonly IHttpClientFactory httpClientFactory;
    private readonly SsrSettings settings;
    private readonly ILogger<HttpTechnicalStatsProvider> logger;

    public HttpTechnicalStatsProvider(
        IHttpClientFactory httpClientFactory,
        SsrSettings settings,
        ILogger<HttpTechnicalStatsProvider> logger)
    {
        this.httpClientFactory = httpClientFactory;
        this.settings = settings;
        this.logger = logger;
    }

    public async Task<TechnicalStatsSnapshot?> GetSnapshotAsync(CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.settings.InternalBaseUrl) || string.IsNullOrWhiteSpace(this.settings.CacheInvalidationToken))
        {
            this.logger.LogDebug("SSR technical stats skipped: SSR internal base URL or token is not configured.");
            return BuildUnavailableSnapshot("missing-settings");
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildTechnicalStatsUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Get, requestUri);

            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR technical stats returned HTTP {StatusCode}.", (int)response.StatusCode);
                return BuildUnavailableSnapshot($"http-{(int)response.StatusCode}");
            }

            string responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            if (string.IsNullOrWhiteSpace(responseBody))
            {
                return BuildUnavailableSnapshot("empty-response");
            }

            return ReadTechnicalStatsSnapshot(responseBody);
        }
        catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
        {
            this.logger.LogWarning(exception, "SSR technical stats request timed out.");
            return BuildUnavailableSnapshot("timeout");
        }
        catch (HttpRequestException exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats request failed.");
            return BuildUnavailableSnapshot("request-failed");
        }
        catch (JsonException exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats returned an invalid snapshot.");
            return BuildUnavailableSnapshot("invalid-json");
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats request failed.");
            return BuildUnavailableSnapshot("unexpected-error");
        }
    }

    public async Task<TechnicalStatsSettings?> UpdateSettingsAsync(TechnicalStatsSettings settings, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(this.settings.InternalBaseUrl) || string.IsNullOrWhiteSpace(this.settings.CacheInvalidationToken))
        {
            this.logger.LogDebug("SSR technical stats settings skipped: SSR internal base URL or token is not configured.");
            return null;
        }

        try
        {
            HttpClient client = this.httpClientFactory.CreateClient(HttpClientName);
            string requestUri = BuildTechnicalStatsSettingsUri(this.settings.InternalBaseUrl);

            using HttpRequestMessage httpRequest = this.CreateAuthorizedRequest(HttpMethod.Put, requestUri);
            httpRequest.Content = JsonContent.Create(settings, options: JsonSerializerOptions.Web);

            using HttpResponseMessage response = await client.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                this.logger.LogWarning("SSR technical stats settings returned HTTP {StatusCode}.", (int)response.StatusCode);
                return null;
            }

            return await response.Content.ReadFromJsonAsync<TechnicalStatsSettings>(JsonSerializerOptions.Web, cancellationToken);
        }
        catch (Exception exception)
        {
            this.logger.LogWarning(exception, "SSR technical stats settings request failed.");
            return null;
        }
    }

    private HttpRequestMessage CreateAuthorizedRequest(HttpMethod method, string requestUri)
    {
        HttpRequestMessage httpRequest = new HttpRequestMessage(method, requestUri);
        httpRequest.Headers.TryAddWithoutValidation(TokenHeaderName, this.settings.CacheInvalidationToken);
        return httpRequest;
    }

    private static string BuildTechnicalStatsUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{TechnicalStatsPath}";
    }

    private static string BuildTechnicalStatsSettingsUri(string baseUrl)
    {
        return $"{baseUrl.TrimEnd('/')}{TechnicalStatsSettingsPath}";
    }

    private static TechnicalStatsSnapshot ReadTechnicalStatsSnapshot(string responseBody)
    {
        using JsonDocument document = JsonDocument.Parse(responseBody);
        JsonElement root = document.RootElement;

        if (root.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException("The SSR technical stats snapshot root is not an object.");
        }

        DateTime now = DateTime.UtcNow;

        TechnicalStatsSnapshot snapshot = new TechnicalStatsSnapshot
        {
            IsAvailable = ReadBoolean(root, "isAvailable", true),
            UnavailableReason = ReadNullableString(root, "unavailableReason"),
            GeneratedAtUtc = ReadDateTime(root, "generatedAtUtc", now),
            StartedAtUtc = ReadDateTime(root, "startedAtUtc", now),
            UptimeSeconds = ReadLong(root, "uptimeSeconds"),
            BuildVersion = ReadString(root, "buildVersion", string.Empty)
        };

        if (TryGetObject(root, "cache", out JsonElement cache))
        {
            snapshot.Cache = new TechnicalStatsCacheSummary
            {
                PageResponses = ReadLong(cache, "pageResponses"),
                CacheablePageResponses = ReadLong(cache, "cacheablePageResponses"),
                CacheHitResponses = ReadLong(cache, "cacheHitResponses"),
                HitRatePercent = ReadDouble(cache, "hitRatePercent"),
                RobotPageResponses = ReadLong(cache, "robotPageResponses"),
                RobotCacheHitResponses = ReadLong(cache, "robotCacheHitResponses"),
                RobotHitRatePercent = ReadDouble(cache, "robotHitRatePercent"),
                Statuses = ReadCountRows(cache, "statuses"),
                RobotFamilies = ReadRobotFamilyRows(cache, "robotFamilies")
            };
        }

        if (TryGetObject(root, "storage", out JsonElement storage))
        {
            snapshot.Storage = new TechnicalStatsStorageSummary
            {
                MemoryEntries = ReadInt32(storage, "memoryEntries"),
                MemoryMaxEntries = ReadInt32(storage, "memoryMaxEntries"),
                DiskEnabled = ReadBoolean(storage, "diskEnabled", false),
                DiskEntries = ReadInt32(storage, "diskEntries"),
                DiskBytes = ReadLong(storage, "diskBytes"),
                DiskMaxBytes = ReadLong(storage, "diskMaxBytes"),
                DiskWrites = ReadLong(storage, "diskWrites"),
                TechnicalStatsPersistenceEntries = ReadInt32(storage, "technicalStatsPersistenceEntries"),
                TechnicalStatsPersistenceBytes = ReadLong(storage, "technicalStatsPersistenceBytes"),
                TechnicalStatsPersistencePurgedBuckets = ReadInt32(storage, "technicalStatsPersistencePurgedBuckets"),
                SeoDocumentEntries = ReadInt32(storage, "seoDocumentEntries"),
                SeoDocumentMaxEntries = ReadInt32(storage, "seoDocumentMaxEntries"),
                SeoDocumentRequests = ReadLong(storage, "seoDocumentRequests"),
                SeoDocumentHits = ReadLong(storage, "seoDocumentHits"),
                SeoDocumentMisses = ReadLong(storage, "seoDocumentMisses"),
                AssetMisses = ReadLong(storage, "assetMisses")
            };
        }

        if (TryGetObject(root, "seo", out JsonElement seo))
        {
            snapshot.Seo = new TechnicalStatsSeoSummary
            {
                RobotNoJsHtmlEnabled = ReadBoolean(seo, "robotNoJsHtmlEnabled", false),
                HtmlResponses = ReadLong(seo, "htmlResponses"),
                SeoReadyHtmlResponses = ReadLong(seo, "seoReadyHtmlResponses"),
                SeoNotReadyHtmlResponses = ReadLong(seo, "seoNotReadyHtmlResponses"),
                SeoReadyRatePercent = ReadDouble(seo, "seoReadyRatePercent"),
                RobotHtmlResponses = ReadLong(seo, "robotHtmlResponses"),
                RobotSeoReadyHtmlResponses = ReadLong(seo, "robotSeoReadyHtmlResponses"),
                RobotSeoNotReadyHtmlResponses = ReadLong(seo, "robotSeoNotReadyHtmlResponses"),
                RobotSeoReadyRatePercent = ReadDouble(seo, "robotSeoReadyRatePercent"),
                RobotNoJsHtmlResponses = ReadLong(seo, "robotNoJsHtmlResponses"),
                RobotHtmlBlockedNotSeoReady = ReadLong(seo, "robotHtmlBlockedNotSeoReady"),
                RobotHtmlNotAllowed = ReadLong(seo, "robotHtmlNotAllowed"),
                RobotSsrUnavailableResponses = ReadLong(seo, "robotSsrUnavailableResponses"),
                RobotCacheOnlyMissResponses = ReadLong(seo, "robotCacheOnlyMissResponses"),
                RobotPageResponses = ReadLong(seo, "robotPageResponses"),
                RobotCacheHitResponses = ReadLong(seo, "robotCacheHitResponses"),
                RobotHitRatePercent = ReadDouble(seo, "robotHitRatePercent"),
                SeoDocumentRequests = ReadLong(seo, "seoDocumentRequests"),
                SeoDocumentHits = ReadLong(seo, "seoDocumentHits"),
                SeoDocumentMisses = ReadLong(seo, "seoDocumentMisses"),
                SeoDocumentHitRatePercent = ReadDouble(seo, "seoDocumentHitRatePercent"),
                QueueFullRejections = ReadLong(seo, "queueFullRejections")
            };
        }

        if (TryGetObject(root, "rendering", out JsonElement rendering))
        {
            snapshot.Rendering = new TechnicalStatsRenderingSummary
            {
                SsrRenderEnabled = ReadBoolean(rendering, "ssrRenderEnabled", false),
                RenderOnCacheMiss = ReadBoolean(rendering, "renderOnCacheMiss", false),
                RenderCriticalRoutesOnCacheMiss = ReadBoolean(rendering, "renderCriticalRoutesOnCacheMiss", false),
                ActiveRenders = ReadInt32(rendering, "activeRenders"),
                QueuedRenders = ReadInt32(rendering, "queuedRenders"),
                MaxConcurrency = ReadInt32(rendering, "maxConcurrency"),
                MaxQueueEntries = ReadInt32(rendering, "maxQueueEntries"),
                TotalRenders = ReadLong(rendering, "totalRenders"),
                AverageRenderMilliseconds = ReadLong(rendering, "averageRenderMilliseconds"),
                MaxRenderMilliseconds = ReadLong(rendering, "maxRenderMilliseconds"),
                SlowRenders = ReadLong(rendering, "slowRenders"),
                SlowRenderThresholdMilliseconds = ReadLong(rendering, "slowRenderThresholdMilliseconds"),
                QueueFullRejections = ReadLong(rendering, "queueFullRejections")
            };
        }

        if (TryGetObject(root, "refresh", out JsonElement refresh))
        {
            snapshot.Refresh = new TechnicalStatsRefreshSummary
            {
                Enabled = ReadBoolean(refresh, "enabled", false),
                PendingRefreshes = ReadInt32(refresh, "pendingRefreshes"),
                ActiveRefreshes = ReadInt32(refresh, "activeRefreshes"),
                DeduplicatedRefreshKeys = ReadInt32(refresh, "deduplicatedRefreshKeys"),
                QueuedRefreshes = ReadLong(refresh, "queuedRefreshes"),
                SucceededRefreshes = ReadLong(refresh, "succeededRefreshes"),
                FailedRefreshes = ReadLong(refresh, "failedRefreshes"),
                MaxUrls = ReadInt32(refresh, "maxUrls"),
                Concurrency = ReadInt32(refresh, "concurrency"),
                DelayMilliseconds = ReadLong(refresh, "delayMilliseconds"),
                TimeoutSeconds = ReadLong(refresh, "timeoutSeconds")
            };
        }

        if (TryGetObject(root, "invalidation", out JsonElement invalidation))
        {
            snapshot.Invalidation = new TechnicalStatsInvalidationSummary
            {
                Requests = ReadLong(invalidation, "requests"),
                AllRequests = ReadLong(invalidation, "allRequests"),
                TargetedRequests = ReadLong(invalidation, "targetedRequests"),
                ClearedEntries = ReadLong(invalidation, "clearedEntries"),
                StaleEntries = ReadLong(invalidation, "staleEntries"),
                QueuedRefreshes = ReadLong(invalidation, "queuedRefreshes"),
                LastInvalidationUtc = ReadNullableDateTime(invalidation, "lastInvalidationUtc")
            };
        }

        if (TryGetObject(root, "config", out JsonElement config))
        {
            snapshot.Config = new TechnicalStatsRuntimeConfig
            {
                PageCacheTtlSeconds = ReadLong(config, "pageCacheTtlSeconds"),
                StalePageCacheSeconds = ReadLong(config, "stalePageCacheSeconds"),
                PageCacheMaxHtmlBytes = ReadLong(config, "pageCacheMaxHtmlBytes"),
                PageCacheBrowserCacheControl = ReadString(config, "pageCacheBrowserCacheControl", string.Empty),
                CsrFallbackCacheControl = ReadString(config, "csrFallbackCacheControl", string.Empty),
                SeoDocumentBrowserCacheControl = ReadString(config, "seoDocumentBrowserCacheControl", string.Empty),
                TechnicalStatsPersistenceEnabled = ReadBoolean(config, "technicalStatsPersistenceEnabled", false),
                TechnicalStatsPersistenceRetentionDays = ReadInt32(config, "technicalStatsPersistenceRetentionDays"),
                TechnicalStatsPersistenceFlushIntervalSeconds = ReadInt32(config, "technicalStatsPersistenceFlushIntervalSeconds"),
                TechnicalStatsPersistenceLastFlushUtc = ReadNullableDateTime(config, "technicalStatsPersistenceLastFlushUtc"),
                TechnicalStatsPersistenceLastCleanupUtc = ReadNullableDateTime(config, "technicalStatsPersistenceLastCleanupUtc")
            };
        }

        return snapshot;
    }

    private static IReadOnlyCollection<TechnicalStatsCount> ReadCountRows(JsonElement parent, string propertyName)
    {
        if (!TryGetArray(parent, propertyName, out JsonElement array))
        {
            return Array.Empty<TechnicalStatsCount>();
        }

        List<TechnicalStatsCount> rows = new List<TechnicalStatsCount>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string key = ReadString(item, "key", string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            rows.Add(new TechnicalStatsCount
            {
                Key = key,
                Count = ReadLong(item, "count"),
                Percent = ReadDouble(item, "percent")
            });

            if (rows.Count >= MaxDistributionRows)
            {
                break;
            }
        }

        return rows.ToArray();
    }

    private static IReadOnlyCollection<TechnicalStatsRobotFamily> ReadRobotFamilyRows(JsonElement parent, string propertyName)
    {
        if (!TryGetArray(parent, propertyName, out JsonElement array))
        {
            return Array.Empty<TechnicalStatsRobotFamily>();
        }

        List<TechnicalStatsRobotFamily> rows = new List<TechnicalStatsRobotFamily>();
        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string key = ReadString(item, "key", string.Empty);
            if (string.IsNullOrWhiteSpace(key))
            {
                continue;
            }

            rows.Add(new TechnicalStatsRobotFamily
            {
                Key = key,
                Category = ReadString(item, "category", string.Empty),
                Count = ReadLong(item, "count"),
                CacheHits = ReadLong(item, "cacheHits"),
                HitRatePercent = ReadDouble(item, "hitRatePercent"),
                SeoReadyResponses = ReadLong(item, "seoReadyResponses"),
                SeoNotReadyResponses = ReadLong(item, "seoNotReadyResponses"),
                SeoReadyRatePercent = ReadDouble(item, "seoReadyRatePercent"),
                NoJsResponses = ReadLong(item, "noJsResponses"),
                BlockedNotSeoReadyResponses = ReadLong(item, "blockedNotSeoReadyResponses"),
                HtmlNotAllowedResponses = ReadLong(item, "htmlNotAllowedResponses"),
                SsrUnavailableResponses = ReadLong(item, "ssrUnavailableResponses")
            });

            if (rows.Count >= MaxDistributionRows)
            {
                break;
            }
        }

        return rows.ToArray();
    }

    private static bool TryGetObject(JsonElement parent, string propertyName, out JsonElement value)
    {
        if (TryGetProperty(parent, propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.Object)
        {
            value = property;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetArray(JsonElement parent, string propertyName, out JsonElement value)
    {
        if (TryGetProperty(parent, propertyName, out JsonElement property) && property.ValueKind == JsonValueKind.Array)
        {
            value = property;
            return true;
        }

        value = default;
        return false;
    }

    private static bool TryGetProperty(JsonElement parent, string propertyName, out JsonElement value)
    {
        if (parent.ValueKind == JsonValueKind.Object && parent.TryGetProperty(propertyName, out JsonElement property))
        {
            value = property;
            return true;
        }

        value = default;
        return false;
    }

    private static string ReadString(JsonElement parent, string propertyName, string fallback)
    {
        if (!TryGetProperty(parent, propertyName, out JsonElement property))
        {
            return fallback;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString() ?? fallback;
        }

        return fallback;
    }

    private static string? ReadNullableString(JsonElement parent, string propertyName)
    {
        if (!TryGetProperty(parent, propertyName, out JsonElement property))
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.Null || property.ValueKind == JsonValueKind.Undefined)
        {
            return null;
        }

        if (property.ValueKind == JsonValueKind.String)
        {
            return property.GetString();
        }

        return null;
    }

    private static bool ReadBoolean(JsonElement parent, string propertyName, bool fallback)
    {
        if (!TryGetProperty(parent, propertyName, out JsonElement property))
        {
            return fallback;
        }

        if (property.ValueKind == JsonValueKind.True)
        {
            return true;
        }

        if (property.ValueKind == JsonValueKind.False)
        {
            return false;
        }

        if (property.ValueKind == JsonValueKind.String && bool.TryParse(property.GetString(), out bool parsed))
        {
            return parsed;
        }

        return fallback;
    }

    private static int ReadInt32(JsonElement parent, string propertyName)
    {
        long value = ReadLong(parent, propertyName);
        if (value > int.MaxValue)
        {
            return int.MaxValue;
        }

        return (int)value;
    }

    private static long ReadLong(JsonElement parent, string propertyName)
    {
        double value = ReadDouble(parent, propertyName);
        if (value <= 0)
        {
            return 0;
        }

        if (value >= long.MaxValue)
        {
            return long.MaxValue;
        }

        return Convert.ToInt64(Math.Round(value, MidpointRounding.AwayFromZero));
    }

    private static double ReadDouble(JsonElement parent, string propertyName)
    {
        if (!TryGetProperty(parent, propertyName, out JsonElement property))
        {
            return 0;
        }

        double value = property.ValueKind switch
        {
            JsonValueKind.Number when property.TryGetDouble(out double parsedNumber) => parsedNumber,
            JsonValueKind.String when double.TryParse(property.GetString(), NumberStyles.Float, CultureInfo.InvariantCulture, out double parsedString) => parsedString,
            _ => 0
        };

        if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
        {
            return 0;
        }

        return value;
    }

    private static DateTime ReadDateTime(JsonElement parent, string propertyName, DateTime fallback)
    {
        return ReadNullableDateTime(parent, propertyName) ?? fallback;
    }

    private static DateTime? ReadNullableDateTime(JsonElement parent, string propertyName)
    {
        if (!TryGetProperty(parent, propertyName, out JsonElement property) || property.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        string? value = property.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        if (DateTime.TryParse(
            value,
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out DateTime parsed))
        {
            return parsed;
        }

        return null;
    }

    private static TechnicalStatsSnapshot BuildUnavailableSnapshot(string reason)
    {
        DateTime now = DateTime.UtcNow;

        return new TechnicalStatsSnapshot
        {
            IsAvailable = false,
            UnavailableReason = reason,
            GeneratedAtUtc = now,
            StartedAtUtc = now,
            UptimeSeconds = 0
        };
    }
}
