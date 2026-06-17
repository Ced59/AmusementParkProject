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

public sealed class VideoRepository : IVideoRepository
{
    private static readonly TimeSpan ReadCacheDuration = TimeSpan.FromMinutes(5);
    private static long cacheVersion;
    private readonly IMongoCollection<VideoDocument> collection;
    private readonly IMemoryCache cache;

    public VideoRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<VideoDocument>(settings.VideosCollectionName);
        this.cache = cache;
    }

    public async Task<PagedResult<Video>> GetPageAsync(int page, int pageSize, VideoSearchCriteria criteria, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        FilterDefinition<VideoDocument> filter = BuildFilter(criteria);
        SortDefinition<VideoDocument> sort = BuildSort(criteria);
        string cacheKey = BuildPageCacheKey(safePage, safePageSize, criteria);

        if (this.cache.TryGetValue(cacheKey, out PagedResult<Video>? cachedPage) && cachedPage is not null)
        {
            return cachedPage;
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<VideoDocument> documents = await this.collection
            .Find(filter)
            .Sort(sort)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        PagedResult<Video> pageResult = new PagedResult<Video>(documents.Select(static document => document.ToDomain()).ToList(), safePage, safePageSize, totalItems);
        this.cache.Set(cacheKey, pageResult, ReadCacheDuration);
        return pageResult;
    }

    public async Task<Video?> GetByIdAsync(string videoId, CancellationToken cancellationToken)
    {
        string cacheKey = BuildByIdCacheKey(videoId);
        if (this.cache.TryGetValue(cacheKey, out Video? cachedVideo) && cachedVideo is not null)
        {
            return cachedVideo;
        }

        VideoDocument? document = await this.collection.Find(item => item.Id == videoId)
            .FirstOrDefaultAsync(cancellationToken);

        Video? video = document?.ToDomain();
        if (video is not null)
        {
            this.cache.Set(cacheKey, video, ReadCacheDuration);
        }

        return video;
    }

    public async Task<Video> CreateAsync(Video video, CancellationToken cancellationToken)
    {
        DateTime now = DateTime.UtcNow;
        video.Id = Guid.NewGuid().ToString("N");
        video.CreatedAtUtc = now;
        video.UpdatedAtUtc = now;
        video.OwnerId = string.IsNullOrWhiteSpace(video.OwnerId) ? null : video.OwnerId.Trim();
        video.TagIds = video.TagIds.Distinct(StringComparer.Ordinal).ToList();

        VideoDocument document = video.ToDocument();
        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        InvalidateReadCache();
        return document.ToDomain();
    }

    public async Task<Video?> UpdateAsync(string videoId, Video video, CancellationToken cancellationToken)
    {
        FilterDefinition<VideoDocument> filter = Builders<VideoDocument>.Filter.Eq(static document => document.Id, videoId);
        UpdateDefinition<VideoDocument> update = Builders<VideoDocument>.Update
            .Set(static document => document.HostingProvider, video.HostingProvider)
            .Set(static document => document.OwnerType, video.OwnerType)
            .Set(static document => document.OwnerId, string.IsNullOrWhiteSpace(video.OwnerId) ? null : video.OwnerId.Trim())
            .Set(static document => document.Type, video.Type)
            .Set(static document => document.OriginalUrl, video.OriginalUrl)
            .Set(static document => document.CanonicalUrl, video.CanonicalUrl)
            .Set(static document => document.EmbedUrl, video.EmbedUrl)
            .Set(static document => document.ExternalId, video.ExternalId)
            .Set(static document => document.Title, video.Title)
            .Set(static document => document.Description, video.Description)
            .Set(static document => document.CreatorName, video.CreatorName)
            .Set(static document => document.CreatorUrl, video.CreatorUrl)
            .Set(static document => document.ThumbnailUrl, video.ThumbnailUrl)
            .Set(static document => document.ThumbnailImageId, video.ThumbnailImageId)
            .Set(static document => document.DurationSeconds, video.Duration.HasValue ? checked((long)video.Duration.Value.TotalSeconds) : null)
            .Set(static document => document.PublishedAtUtc, video.PublishedAtUtc)
            .Set(static document => document.Titles, CommonMongoMappers.ToDocuments(video.Titles))
            .Set(static document => document.Descriptions, CommonMongoMappers.ToDocuments(video.Descriptions))
            .Set(static document => document.TagIds, video.TagIds.Distinct(StringComparer.Ordinal).ToList())
            .Set(static document => document.ExternalMetadata, video.ExternalMetadata.ToDocument())
            .Set(static document => document.IsPublished, video.IsPublished)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<VideoDocument> options = new FindOneAndUpdateOptions<VideoDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        VideoDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    public async Task<Video?> SetThumbnailImageAsync(string videoId, string thumbnailImageId, CancellationToken cancellationToken)
    {
        FilterDefinition<VideoDocument> filter = Builders<VideoDocument>.Filter.Eq(static document => document.Id, videoId);
        UpdateDefinition<VideoDocument> update = Builders<VideoDocument>.Update
            .Set(static document => document.ThumbnailImageId, thumbnailImageId)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<VideoDocument> options = new FindOneAndUpdateOptions<VideoDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        VideoDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    public async Task<bool> DeleteAsync(string videoId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == videoId, cancellationToken: cancellationToken);
        if (result.DeletedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.DeletedCount > 0;
    }

    private static FilterDefinition<VideoDocument> BuildFilter(VideoSearchCriteria criteria)
    {
        FilterDefinitionBuilder<VideoDocument> builder = Builders<VideoDocument>.Filter;
        FilterDefinition<VideoDocument> filter = builder.Empty;

        if (criteria.HostingProvider.HasValue)
        {
            filter &= builder.Eq(static document => document.HostingProvider, criteria.HostingProvider.Value);
        }

        if (criteria.OwnerType.HasValue)
        {
            filter &= BuildOwnerTypeFilter(builder, criteria.OwnerType.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId))
        {
            filter &= builder.Eq(static document => document.OwnerId, criteria.OwnerId.Trim());
        }

        if (criteria.Type.HasValue)
        {
            filter &= builder.Eq(static document => document.Type, criteria.Type.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.TagId))
        {
            filter &= builder.AnyEq(static document => document.TagIds, criteria.TagId.Trim());
        }

        if (criteria.IsPublished.HasValue)
        {
            filter &= builder.Eq(static document => document.IsPublished, criteria.IsPublished.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.CreatorName))
        {
            BsonRegularExpression creatorRegex = new BsonRegularExpression(Regex.Escape(criteria.CreatorName.Trim()), "i");
            filter &= builder.Regex(static document => document.CreatorName, creatorRegex);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            BsonRegularExpression regex = new BsonRegularExpression(Regex.Escape(criteria.Search.Trim()), "i");
            filter &= builder.Or(
                builder.Regex(static document => document.Id, regex),
                builder.Regex(static document => document.Title, regex),
                builder.Regex(static document => document.Description, regex),
                builder.Regex(static document => document.CreatorName, regex),
                builder.Regex(static document => document.CanonicalUrl, regex),
                builder.Regex(static document => document.ExternalId, regex),
                builder.Regex("titles.value", regex),
                builder.Regex("descriptions.value", regex));
        }

        return filter;
    }

    private static FilterDefinition<VideoDocument> BuildOwnerTypeFilter(FilterDefinitionBuilder<VideoDocument> builder, VideoOwnerType ownerType)
    {
        FilterDefinition<VideoDocument> currentFilter = builder.Eq(static document => document.OwnerType, ownerType);
        return ownerType == VideoOwnerType.ParkItem
            ? builder.Or(currentFilter, builder.Eq("ownerType", "Attraction"))
            : currentFilter;
    }

    private static SortDefinition<VideoDocument> BuildSort(VideoSearchCriteria criteria)
    {
        bool descending = !string.Equals(criteria.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        SortDefinitionBuilder<VideoDocument> builder = Builders<VideoDocument>.Sort;

        return (criteria.SortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("title", true) => builder.Descending(static document => document.Title).Descending(static document => document.CreatedAt),
            ("title", false) => builder.Ascending(static document => document.Title).Ascending(static document => document.CreatedAt),
            ("published", true) => builder.Descending(static document => document.PublishedAtUtc).Descending(static document => document.CreatedAt),
            ("published", false) => builder.Ascending(static document => document.PublishedAtUtc).Ascending(static document => document.CreatedAt),
            ("updated", true) => builder.Descending(static document => document.UpdatedAt),
            ("updated", false) => builder.Ascending(static document => document.UpdatedAt),
            ("created", false) => builder.Ascending(static document => document.CreatedAt),
            _ => builder.Descending(static document => document.CreatedAt),
        };
    }

    private static string BuildByIdCacheKey(string videoId)
    {
        string normalizedVideoId = string.IsNullOrWhiteSpace(videoId) ? string.Empty : videoId.Trim();
        return $"videos:by-id:{GetCacheVersion()}:{normalizedVideoId}";
    }

    private static string BuildPageCacheKey(int page, int pageSize, VideoSearchCriteria criteria)
    {
        return string.Join(
            ":",
            "videos:page",
            GetCacheVersion().ToString(System.Globalization.CultureInfo.InvariantCulture),
            page.ToString(System.Globalization.CultureInfo.InvariantCulture),
            pageSize.ToString(System.Globalization.CultureInfo.InvariantCulture),
            criteria.Search,
            criteria.HostingProvider,
            criteria.OwnerType,
            criteria.OwnerId,
            criteria.Type,
            criteria.TagId,
            criteria.CreatorName,
            criteria.IsPublished,
            criteria.SortBy,
            criteria.SortDirection);
    }

    private static long GetCacheVersion()
    {
        return Volatile.Read(ref cacheVersion);
    }

    private static void InvalidateReadCache()
    {
        Interlocked.Increment(ref cacheVersion);
    }
}

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

        List<VideoTagDocument> documents = await this.collection.Find(Builders<VideoTagDocument>.Filter.Empty)
            .SortBy(static document => document.Slug)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<VideoTag> tags = documents.Select(static document => document.ToDomain()).ToList();
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
