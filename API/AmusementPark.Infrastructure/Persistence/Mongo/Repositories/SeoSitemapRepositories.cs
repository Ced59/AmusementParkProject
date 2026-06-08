using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class SeoSitemapSnapshotRepository : ISeoSitemapSnapshotRepository
{
    private const string CurrentSnapshotId = "current";
    private const string CurrentSnapshotCacheKey = "seo:sitemap:snapshot:current";
    private const string MissingSnapshotCacheKey = "seo:sitemap:snapshot:missing";
    private static readonly TimeSpan SnapshotCacheDuration = TimeSpan.FromMinutes(10);
    private readonly IMongoCollection<SeoSitemapSnapshotDocument> collection;
    private readonly IMemoryCache cache;

    public SeoSitemapSnapshotRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<SeoSitemapSnapshotDocument>(settings.SeoSitemapSnapshotsCollectionName);
        this.cache = cache;
    }

    public async Task<SitemapSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        if (this.cache.TryGetValue(CurrentSnapshotCacheKey, out SitemapSnapshot? cachedSnapshot) && cachedSnapshot is not null)
        {
            return cachedSnapshot;
        }

        if (this.cache.TryGetValue(MissingSnapshotCacheKey, out bool isMissingSnapshotCached) && isMissingSnapshotCached)
        {
            return null;
        }

        SeoSitemapSnapshotDocument? document = await this.collection
            .Find(document => document.Id == CurrentSnapshotId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            this.cache.Set(MissingSnapshotCacheKey, true, TimeSpan.FromSeconds(30));
            return null;
        }

        SitemapSnapshot snapshot = ToSnapshot(document);
        this.cache.Set(CurrentSnapshotCacheKey, snapshot, SnapshotCacheDuration);
        return snapshot;
    }

    public async Task SaveAsync(SitemapSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        SeoSitemapSnapshotDocument document = new SeoSitemapSnapshotDocument
        {
            Id = CurrentSnapshotId,
            CreatedAt = snapshot.GeneratedAtUtc,
            UpdatedAt = snapshot.GeneratedAtUtc,
            GeneratedAtUtc = snapshot.GeneratedAtUtc,
            PublicBaseUrl = snapshot.PublicBaseUrl,
            IndexXml = snapshot.IndexXml,
            SectionXmlByKey = new Dictionary<string, string>(snapshot.SectionXmlByKey, StringComparer.OrdinalIgnoreCase),
            Sections = snapshot.Sections.Select(ToDocument).ToList(),
            TotalUrlCount = snapshot.TotalUrlCount,
        };

        ReplaceOptions options = new ReplaceOptions { IsUpsert = true };
        await this.collection.ReplaceOneAsync(value => value.Id == CurrentSnapshotId, document, options, cancellationToken);
        this.cache.Remove(MissingSnapshotCacheKey);
        this.cache.Set(CurrentSnapshotCacheKey, snapshot, SnapshotCacheDuration);
    }

    private static SitemapSnapshot ToSnapshot(SeoSitemapSnapshotDocument document)
    {
        return new SitemapSnapshot
        {
            Id = document.Id,
            GeneratedAtUtc = document.GeneratedAtUtc,
            PublicBaseUrl = document.PublicBaseUrl,
            IndexXml = document.IndexXml,
            SectionXmlByKey = new Dictionary<string, string>(document.SectionXmlByKey, StringComparer.OrdinalIgnoreCase),
            Sections = document.Sections.Select(ToModel).ToList(),
            TotalUrlCount = document.TotalUrlCount,
        };
    }

    private static SeoSitemapSectionStatsDocument ToDocument(SitemapSectionStats value)
    {
        return new SeoSitemapSectionStatsDocument
        {
            Key = value.Key,
            FileName = value.FileName,
            DisplayName = value.DisplayName,
            UrlCount = value.UrlCount,
            LastModifiedUtc = value.LastModifiedUtc,
        };
    }

    private static SitemapSectionStats ToModel(SeoSitemapSectionStatsDocument value)
    {
        return new SitemapSectionStats(value.Key, value.FileName, value.DisplayName, value.UrlCount, value.LastModifiedUtc);
    }
}

public sealed class SeoSitemapGenerationHistoryRepository : ISeoSitemapGenerationHistoryRepository
{
    private readonly IMongoCollection<SeoSitemapGenerationHistoryDocument> collection;

    public SeoSitemapGenerationHistoryRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<SeoSitemapGenerationHistoryDocument>(settings.SeoSitemapGenerationHistoryCollectionName);
    }

    public async Task WriteAsync(SitemapGenerationHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        SeoSitemapGenerationHistoryDocument document = new SeoSitemapGenerationHistoryDocument
        {
            Id = entry.Id,
            CreatedAt = entry.StartedAtUtc,
            UpdatedAt = entry.CompletedAtUtc ?? DateTime.UtcNow,
            StartedAtUtc = entry.StartedAtUtc,
            CompletedAtUtc = entry.CompletedAtUtc,
            DurationMs = entry.DurationMs,
            Status = entry.Status.ToString(),
            Trigger = entry.Trigger.ToString(),
            TriggeredByUserId = entry.TriggeredByUserId,
            TriggeredByUserEmail = entry.TriggeredByUserEmail,
            TotalUrlCount = entry.TotalUrlCount,
            Sections = entry.Sections.Select(ToDocument).ToList(),
            Errors = entry.Errors.ToList(),
            IndexNow = ToDocument(entry.IndexNow),
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }

    public async Task<PagedResult<SitemapGenerationHistoryEntry>> SearchAsync(PagedQuery paging, CancellationToken cancellationToken)
    {
        int skip = (paging.Page - 1) * paging.PageSize;
        long totalItems = await this.collection.CountDocumentsAsync(Builders<SeoSitemapGenerationHistoryDocument>.Filter.Empty, cancellationToken: cancellationToken);
        List<SeoSitemapGenerationHistoryDocument> documents = await this.collection
            .Find(Builders<SeoSitemapGenerationHistoryDocument>.Filter.Empty)
            .SortByDescending(static document => document.StartedAtUtc)
            .Skip(skip)
            .Limit(paging.PageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<SitemapGenerationHistoryEntry> items = documents.Select(ToModel).ToList();
        return new PagedResult<SitemapGenerationHistoryEntry>(items, paging.Page, paging.PageSize, totalItems);
    }

    private static SeoSitemapSectionStatsDocument ToDocument(SitemapSectionStats value)
    {
        return new SeoSitemapSectionStatsDocument
        {
            Key = value.Key,
            FileName = value.FileName,
            DisplayName = value.DisplayName,
            UrlCount = value.UrlCount,
            LastModifiedUtc = value.LastModifiedUtc,
        };
    }

    private static SeoIndexNowSubmissionDocument ToDocument(IndexNowSubmissionResult value)
    {
        return new SeoIndexNowSubmissionDocument
        {
            WasRequested = value.WasRequested,
            IsEnabled = value.IsEnabled,
            IsSuccess = value.IsSuccess,
            SubmittedUrlCount = value.SubmittedUrlCount,
            AcceptedEndpoints = value.AcceptedEndpoints.ToList(),
            Errors = value.Errors.ToList(),
        };
    }

    private static SitemapGenerationHistoryEntry ToModel(SeoSitemapGenerationHistoryDocument document)
    {
        return new SitemapGenerationHistoryEntry
        {
            Id = document.Id,
            StartedAtUtc = document.StartedAtUtc,
            CompletedAtUtc = document.CompletedAtUtc,
            DurationMs = document.DurationMs,
            Status = Enum.TryParse(document.Status, ignoreCase: true, out SitemapGenerationStatus status) ? status : SitemapGenerationStatus.Failed,
            Trigger = Enum.TryParse(document.Trigger, ignoreCase: true, out SitemapGenerationTrigger trigger) ? trigger : SitemapGenerationTrigger.Manual,
            TriggeredByUserId = document.TriggeredByUserId,
            TriggeredByUserEmail = document.TriggeredByUserEmail,
            TotalUrlCount = document.TotalUrlCount,
            Sections = document.Sections.Select(static value => new SitemapSectionStats(value.Key, value.FileName, value.DisplayName, value.UrlCount, value.LastModifiedUtc)).ToList(),
            Errors = document.Errors.ToList(),
            IndexNow = new IndexNowSubmissionResult
            {
                WasRequested = document.IndexNow.WasRequested,
                IsEnabled = document.IndexNow.IsEnabled,
                IsSuccess = document.IndexNow.IsSuccess,
                SubmittedUrlCount = document.IndexNow.SubmittedUrlCount,
                AcceptedEndpoints = document.IndexNow.AcceptedEndpoints.ToList(),
                Errors = document.IndexNow.Errors.ToList(),
            },
        };
    }
}

public sealed class SeoSitemapSettingsRepository : ISeoSitemapSettingsRepository
{
    private const string SettingsId = "current";
    private const string SettingsCacheKey = "seo:sitemap:settings:current";
    private static readonly TimeSpan SettingsCacheDuration = TimeSpan.FromMinutes(5);
    private readonly IMongoCollection<SeoSitemapSettingsDocument> collection;
    private readonly IMemoryCache cache;

    public SeoSitemapSettingsRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<SeoSitemapSettingsDocument>(settings.SeoSitemapSettingsCollectionName);
        this.cache = cache;
    }

    public async Task<SeoSitemapSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (this.cache.TryGetValue(SettingsCacheKey, out SeoSitemapSettings? cachedSettings) && cachedSettings is not null)
        {
            return cachedSettings;
        }

        SeoSitemapSettingsDocument? document = await this.collection
            .Find(document => document.Id == SettingsId)
            .FirstOrDefaultAsync(cancellationToken);

        SeoSitemapSettings settings = document is null ? CreateDefaultSettings() : ToModel(document);
        this.cache.Set(SettingsCacheKey, settings, SettingsCacheDuration);
        return settings;
    }

    public async Task SaveAsync(SeoSitemapSettings settings, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(settings);

        SeoSitemapSettingsDocument document = new SeoSitemapSettingsDocument
        {
            Id = SettingsId,
            CreatedAt = settings.UpdatedAtUtc,
            UpdatedAt = settings.UpdatedAtUtc,
            IsIndexNowEnabled = settings.IsIndexNowEnabled,
            SubmitToIndexNowAfterManualGeneration = settings.SubmitToIndexNowAfterManualGeneration,
            SubmitToIndexNowAfterAutomaticGeneration = settings.SubmitToIndexNowAfterAutomaticGeneration,
            IndexNowKey = settings.IndexNowKey,
            IndexNowKeyLocation = settings.IndexNowKeyLocation,
            IndexNowEndpoints = settings.IndexNowEndpoints.ToList(),
        };

        ReplaceOptions options = new ReplaceOptions { IsUpsert = true };
        await this.collection.ReplaceOneAsync(value => value.Id == SettingsId, document, options, cancellationToken);
        this.cache.Set(SettingsCacheKey, settings, SettingsCacheDuration);
    }

    private static SeoSitemapSettings CreateDefaultSettings()
    {
        return new SeoSitemapSettings
        {
            IsIndexNowEnabled = false,
            SubmitToIndexNowAfterManualGeneration = false,
            SubmitToIndexNowAfterAutomaticGeneration = false,
            IndexNowKey = string.Empty,
            IndexNowKeyLocation = string.Empty,
            IndexNowEndpoints = new[]
            {
                "https://api.indexnow.org/indexnow",
                "https://www.bing.com/indexnow",
            },
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    private static SeoSitemapSettings ToModel(SeoSitemapSettingsDocument document)
    {
        return new SeoSitemapSettings
        {
            IsIndexNowEnabled = document.IsIndexNowEnabled,
            SubmitToIndexNowAfterManualGeneration = document.SubmitToIndexNowAfterManualGeneration,
            SubmitToIndexNowAfterAutomaticGeneration = document.SubmitToIndexNowAfterAutomaticGeneration,
            IndexNowKey = document.IndexNowKey,
            IndexNowKeyLocation = document.IndexNowKeyLocation,
            IndexNowEndpoints = document.IndexNowEndpoints.Count > 0 ? document.IndexNowEndpoints : CreateDefaultSettings().IndexNowEndpoints,
            UpdatedAtUtc = document.UpdatedAt,
        };
    }
}
