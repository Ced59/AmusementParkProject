using System.Text;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

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
