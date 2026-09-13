using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Videos.Contracts;
using AmusementPark.Application.Features.Videos.Ports;
using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class VideoTagRepository : IVideoTagRepository
{
    private static readonly TimeSpan TagCacheDuration = TimeSpan.FromMinutes(30);
    private static long tagCacheVersion;
    private readonly IMongoCollection<VideoTagDocument> collection;
    private readonly IMemoryCache cache;

    public VideoTagRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<VideoTagDocument>(settings.VideoTagsCollectionName);
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<VideoTag>> GetAllAsync(CancellationToken cancellationToken)
    {
        string cacheKey = BuildAllTagsCacheKey();
        if (this.cache.TryGetValue(cacheKey, out IReadOnlyCollection<VideoTag>? cachedTags) && cachedTags is not null)
        {
            return cachedTags;
        }

        List<BsonDocument> documents = await this.collection.Find(Builders<VideoTagDocument>.Filter.Empty)
            .SortBy(static document => document.Slug)
            .Project<BsonDocument>(Builders<VideoTagDocument>.Projection.Exclude("__unused"))
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<VideoTag> tags = documents.Select(static document => document.ToVideoTagDomain()).ToList();
        this.cache.Set(cacheKey, tags, TagCacheDuration);
        return tags;
    }

    public async Task<VideoTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken)
    {
        VideoTagDocument? document = await this.collection.Find(document => document.Id == tagId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<VideoTag?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        VideoTagDocument? document = await this.collection.Find(document => document.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<VideoTag> CreateAsync(VideoTagWriteModel tag, CancellationToken cancellationToken)
    {
        VideoTagDocument document = new VideoTagDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Slug = tag.Slug,
            Labels = CommonMongoMappers.ToDocuments(tag.Labels),
            Descriptions = CommonMongoMappers.ToDocuments(tag.Descriptions),
            IsActive = tag.IsActive,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        InvalidateTagCache();
        return document.ToDomain();
    }

    public async Task<VideoTag?> UpdateAsync(string tagId, VideoTagWriteModel tag, CancellationToken cancellationToken)
    {
        FilterDefinition<VideoTagDocument> filter = Builders<VideoTagDocument>.Filter.Eq(static document => document.Id, tagId);
        UpdateDefinition<VideoTagDocument> update = Builders<VideoTagDocument>.Update
            .Set(static document => document.Slug, tag.Slug)
            .Set(static document => document.Labels, CommonMongoMappers.ToDocuments(tag.Labels))
            .Set(static document => document.Descriptions, CommonMongoMappers.ToDocuments(tag.Descriptions))
            .Set(static document => document.IsActive, tag.IsActive)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<VideoTagDocument> options = new FindOneAndUpdateOptions<VideoTagDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        VideoTagDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateTagCache();
        }

        return document?.ToDomain();
    }

    private static string BuildAllTagsCacheKey()
    {
        return $"video-tags:all:{Volatile.Read(ref tagCacheVersion)}";
    }

    private static void InvalidateTagCache()
    {
        Interlocked.Increment(ref tagCacheVersion);
    }
}
