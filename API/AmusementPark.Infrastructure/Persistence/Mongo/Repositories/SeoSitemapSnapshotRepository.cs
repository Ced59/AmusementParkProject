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

public sealed class SeoSitemapSnapshotRepository : ISeoSitemapSnapshotRepository
{
    private const string CurrentSnapshotId = "current";
    private const string CurrentSnapshotCacheKey = "seo:sitemap:snapshot:current";
    private const string MissingSnapshotCacheKey = "seo:sitemap:snapshot:missing";
    private const int SectionXmlChunkMaxLength = 2 * 1024 * 1024;
    private const int SectionChunkInsertBatchSize = 100;
    private readonly IMongoCollection<SeoSitemapSnapshotDocument> snapshotsCollection;
    private readonly IMongoCollection<SeoSitemapSnapshotSectionChunkDocument> sectionChunksCollection;
    private readonly IMemoryCache cache;
    private readonly ILogger<SeoSitemapSnapshotRepository> logger;

    public SeoSitemapSnapshotRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IMemoryCache cache,
        ILogger<SeoSitemapSnapshotRepository> logger)
    {
        this.snapshotsCollection = database.GetCollection<SeoSitemapSnapshotDocument>(settings.SeoSitemapSnapshotsCollectionName);
        this.sectionChunksCollection = database.GetCollection<SeoSitemapSnapshotSectionChunkDocument>(settings.SeoSitemapSnapshotSectionsCollectionName);
        this.cache = cache;
        this.logger = logger;
    }

    public async Task<SitemapSnapshot?> GetLatestAsync(CancellationToken cancellationToken)
    {
        SeoSitemapSnapshotCacheEntry? entry = await this.GetCurrentEntryAsync(cancellationToken);
        return entry?.Snapshot;
    }

    public async Task<string?> GetSectionXmlAsync(string sectionKey, CancellationToken cancellationToken)
    {
        string normalizedSectionKey = NormalizeSectionKey(sectionKey);
        if (string.IsNullOrWhiteSpace(normalizedSectionKey))
        {
            return null;
        }

        SeoSitemapSnapshotCacheEntry? entry = await this.GetCurrentEntryAsync(cancellationToken);
        if (entry is null)
        {
            return null;
        }

        string cacheKey = CreateSectionXmlCacheKey(entry.SectionCacheToken, normalizedSectionKey);
        if (this.cache.TryGetValue(cacheKey, out string? cachedSectionXml) && cachedSectionXml is not null)
        {
            return cachedSectionXml;
        }

        string? sectionXml = null;
        if (!string.IsNullOrWhiteSpace(entry.SectionsStorageId))
        {
            sectionXml = await this.LoadChunkedSectionXmlAsync(entry.SectionsStorageId, normalizedSectionKey, cancellationToken);
        }

        if (sectionXml is null && entry.LegacySectionXmlByKey.TryGetValue(normalizedSectionKey, out string? legacySectionXml))
        {
            sectionXml = legacySectionXml;
        }

        if (sectionXml is not null)
        {
            this.cache.Set(cacheKey, sectionXml, CreateSectionCacheOptions());
        }

        return sectionXml;
    }

    public async Task SaveAsync(SitemapSnapshot snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        string sectionsStorageId = Guid.NewGuid().ToString("N");
        List<SeoSitemapSnapshotSectionChunkDocument> sectionChunkDocuments = BuildSectionChunkDocuments(snapshot, sectionsStorageId);
        await this.InsertSectionChunksAsync(sectionChunkDocuments, cancellationToken);

        SeoSitemapSnapshotDocument document = new SeoSitemapSnapshotDocument
        {
            Id = CurrentSnapshotId,
            CreatedAt = snapshot.GeneratedAtUtc,
            UpdatedAt = snapshot.GeneratedAtUtc,
            GeneratedAtUtc = snapshot.GeneratedAtUtc,
            PublicBaseUrl = snapshot.PublicBaseUrl,
            IndexXml = snapshot.IndexXml,
            SectionsStorageId = sectionsStorageId,
            SectionXmlByKey = null,
            Sections = snapshot.Sections.Select(ToDocument).ToList(),
            TotalUrlCount = snapshot.TotalUrlCount,
        };

        ReplaceOptions options = new ReplaceOptions { IsUpsert = true };
        await this.snapshotsCollection.ReplaceOneAsync(value => value.Id == CurrentSnapshotId, document, options, cancellationToken);

        await this.TryDeleteStaleSectionChunksAsync(sectionsStorageId, cancellationToken);

        SeoSitemapSnapshotCacheEntry cacheEntry = ToCacheEntry(document);
        this.cache.Remove(MissingSnapshotCacheKey);
        this.cache.Set(CurrentSnapshotCacheKey, cacheEntry, CreateSnapshotCacheOptions());
    }

    private async Task<SeoSitemapSnapshotCacheEntry?> GetCurrentEntryAsync(CancellationToken cancellationToken)
    {
        if (this.cache.TryGetValue(CurrentSnapshotCacheKey, out SeoSitemapSnapshotCacheEntry? cachedEntry) && cachedEntry is not null)
        {
            return cachedEntry;
        }

        if (this.cache.TryGetValue(MissingSnapshotCacheKey, out bool isMissingSnapshotCached) && isMissingSnapshotCached)
        {
            return null;
        }

        SeoSitemapSnapshotDocument? document = await this.snapshotsCollection
            .Find(document => document.Id == CurrentSnapshotId)
            .FirstOrDefaultAsync(cancellationToken);

        if (document is null)
        {
            this.cache.Set(MissingSnapshotCacheKey, true, TimeSpan.FromSeconds(30));
            return null;
        }

        SeoSitemapSnapshotCacheEntry entry = ToCacheEntry(document);
        this.cache.Set(CurrentSnapshotCacheKey, entry, CreateSnapshotCacheOptions());
        return entry;
    }

    private static MemoryCacheEntryOptions CreateSnapshotCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.NeverRemove,
        };
    }

    private static MemoryCacheEntryOptions CreateSectionCacheOptions()
    {
        return new MemoryCacheEntryOptions
        {
            Priority = CacheItemPriority.Normal,
            SlidingExpiration = TimeSpan.FromMinutes(30),
        };
    }

    private async Task InsertSectionChunksAsync(
        IReadOnlyCollection<SeoSitemapSnapshotSectionChunkDocument> sectionChunkDocuments,
        CancellationToken cancellationToken)
    {
        if (sectionChunkDocuments.Count == 0)
        {
            return;
        }

        List<SeoSitemapSnapshotSectionChunkDocument> batch = new List<SeoSitemapSnapshotSectionChunkDocument>(SectionChunkInsertBatchSize);
        foreach (SeoSitemapSnapshotSectionChunkDocument document in sectionChunkDocuments)
        {
            batch.Add(document);
            if (batch.Count < SectionChunkInsertBatchSize)
            {
                continue;
            }

            await this.sectionChunksCollection.InsertManyAsync(batch, cancellationToken: cancellationToken);
            batch = new List<SeoSitemapSnapshotSectionChunkDocument>(SectionChunkInsertBatchSize);
        }

        if (batch.Count > 0)
        {
            await this.sectionChunksCollection.InsertManyAsync(batch, cancellationToken: cancellationToken);
        }
    }

    private async Task TryDeleteStaleSectionChunksAsync(string currentSectionsStorageId, CancellationToken cancellationToken)
    {
        try
        {
            FilterDefinition<SeoSitemapSnapshotSectionChunkDocument> filter =
                Builders<SeoSitemapSnapshotSectionChunkDocument>.Filter.Eq(document => document.SnapshotId, CurrentSnapshotId)
                & Builders<SeoSitemapSnapshotSectionChunkDocument>.Filter.Ne(document => document.StorageId, currentSectionsStorageId);

            await this.sectionChunksCollection.DeleteManyAsync(filter, cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            this.logger.LogWarning(exception, "Unable to cleanup stale sitemap snapshot section chunks.");
        }
    }

    private async Task<string?> LoadChunkedSectionXmlAsync(string sectionsStorageId, string normalizedSectionKey, CancellationToken cancellationToken)
    {
        FilterDefinition<SeoSitemapSnapshotSectionChunkDocument> filter =
            Builders<SeoSitemapSnapshotSectionChunkDocument>.Filter.Eq(document => document.SnapshotId, CurrentSnapshotId)
            & Builders<SeoSitemapSnapshotSectionChunkDocument>.Filter.Eq(document => document.StorageId, sectionsStorageId)
            & Builders<SeoSitemapSnapshotSectionChunkDocument>.Filter.Eq(document => document.SectionKey, normalizedSectionKey);

        List<SeoSitemapSnapshotSectionChunkDocument> chunks = await this.sectionChunksCollection
            .Find(filter)
            .SortBy(document => document.ChunkIndex)
            .ToListAsync(cancellationToken);

        if (chunks.Count == 0)
        {
            return null;
        }

        int expectedChunkCount = chunks[0].ChunkCount;
        if (expectedChunkCount <= 0 || chunks.Count != expectedChunkCount)
        {
            this.logger.LogWarning(
                "Sitemap section {SectionKey} has an invalid chunk count. Expected {ExpectedChunkCount}, found {ActualChunkCount}.",
                normalizedSectionKey,
                expectedChunkCount,
                chunks.Count);
            return null;
        }

        int totalLength = chunks.Sum(static chunk => chunk.XmlChunk.Length);
        StringBuilder xmlBuilder = new StringBuilder(totalLength);
        for (int index = 0; index < chunks.Count; index++)
        {
            SeoSitemapSnapshotSectionChunkDocument chunk = chunks[index];
            if (chunk.ChunkIndex != index || chunk.ChunkCount != expectedChunkCount)
            {
                this.logger.LogWarning("Sitemap section {SectionKey} has inconsistent chunk metadata.", normalizedSectionKey);
                return null;
            }

            xmlBuilder.Append(chunk.XmlChunk);
        }

        return xmlBuilder.ToString();
    }

    private static SeoSitemapSnapshotCacheEntry ToCacheEntry(SeoSitemapSnapshotDocument document)
    {
        Dictionary<string, string> legacySectionXmlByKey = NormalizeLegacySectionXmlByKey(document.SectionXmlByKey);
        SitemapSnapshot snapshot = new SitemapSnapshot
        {
            Id = document.Id,
            GeneratedAtUtc = document.GeneratedAtUtc,
            PublicBaseUrl = document.PublicBaseUrl,
            IndexXml = document.IndexXml,
            SectionXmlByKey = legacySectionXmlByKey,
            Sections = document.Sections.Select(ToModel).ToList(),
            TotalUrlCount = document.TotalUrlCount,
        };

        return new SeoSitemapSnapshotCacheEntry(
            snapshot,
            document.SectionsStorageId ?? string.Empty,
            legacySectionXmlByKey);
    }

    private static Dictionary<string, string> NormalizeLegacySectionXmlByKey(Dictionary<string, string>? sectionXmlByKey)
    {
        Dictionary<string, string> normalized = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        if (sectionXmlByKey is null)
        {
            return normalized;
        }

        foreach (KeyValuePair<string, string> section in sectionXmlByKey)
        {
            string normalizedSectionKey = NormalizeSectionKey(section.Key);
            if (string.IsNullOrWhiteSpace(normalizedSectionKey))
            {
                continue;
            }

            normalized[normalizedSectionKey] = section.Value ?? string.Empty;
        }

        return normalized;
    }

    private static List<SeoSitemapSnapshotSectionChunkDocument> BuildSectionChunkDocuments(SitemapSnapshot snapshot, string sectionsStorageId)
    {
        List<SeoSitemapSnapshotSectionChunkDocument> documents = new List<SeoSitemapSnapshotSectionChunkDocument>();
        foreach (KeyValuePair<string, string> section in snapshot.SectionXmlByKey)
        {
            string normalizedSectionKey = NormalizeSectionKey(section.Key);
            if (string.IsNullOrWhiteSpace(normalizedSectionKey))
            {
                continue;
            }

            string sectionXml = section.Value ?? string.Empty;
            List<string> chunks = SplitSectionXml(sectionXml).ToList();
            for (int index = 0; index < chunks.Count; index++)
            {
                documents.Add(new SeoSitemapSnapshotSectionChunkDocument
                {
                    Id = $"{CurrentSnapshotId}:{sectionsStorageId}:{normalizedSectionKey}:{index:D6}",
                    CreatedAt = snapshot.GeneratedAtUtc,
                    UpdatedAt = snapshot.GeneratedAtUtc,
                    SnapshotId = CurrentSnapshotId,
                    StorageId = sectionsStorageId,
                    SectionKey = normalizedSectionKey,
                    ChunkIndex = index,
                    ChunkCount = chunks.Count,
                    XmlChunk = chunks[index],
                });
            }
        }

        return documents;
    }

    private static IEnumerable<string> SplitSectionXml(string sectionXml)
    {
        if (sectionXml.Length == 0)
        {
            yield return string.Empty;
            yield break;
        }

        int offset = 0;
        while (offset < sectionXml.Length)
        {
            int length = Math.Min(SectionXmlChunkMaxLength, sectionXml.Length - offset);
            int nextOffset = offset + length;
            if (nextOffset < sectionXml.Length && char.IsHighSurrogate(sectionXml[nextOffset - 1]))
            {
                length--;
            }

            yield return sectionXml.Substring(offset, length);
            offset += length;
        }
    }

    private static string CreateSectionXmlCacheKey(string sectionCacheToken, string normalizedSectionKey)
    {
        return $"seo:sitemap:snapshot:section:{sectionCacheToken}:{normalizedSectionKey}";
    }

    private static string NormalizeSectionKey(string sectionKeyOrFileName)
    {
        if (string.IsNullOrWhiteSpace(sectionKeyOrFileName))
        {
            return string.Empty;
        }

        string value = sectionKeyOrFileName.Trim();
        if (value.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
        {
            value = value[..^4];
        }

        return value.ToLowerInvariant();
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
