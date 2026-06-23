using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Geo;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using System.Text.RegularExpressions;
using System.Threading;
using Microsoft.Extensions.Caching.Memory;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des images.
/// </summary>
public sealed class ImageRepository : IImageRepository
{
    private static readonly TimeSpan OwnerImagesCacheDuration = TimeSpan.FromMinutes(5);
    private static long cacheVersion;
    private readonly IMongoCollection<ImageDocument> collection;
    private readonly IMemoryCache cache;

    public ImageRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<ImageDocument>(settings.ImagesCollectionName);
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<Image>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ImageDocument> documents = await this.collection.Find(Builders<ImageDocument>.Filter.Empty)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<Image>> GetPageAsync(int page, int pageSize, ImageSearchCriteria criteria, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        FilterDefinition<ImageDocument> filter = BuildFilter(criteria);
        SortDefinition<ImageDocument> sort = BuildSort(criteria);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<ImageDocument> documents = await this.collection
            .Find(filter)
            .Sort(sort)
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Image>(documents.Select(static document => document.ToDomain()).ToList(), safePage, safePageSize, totalItems);
    }

    public async Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken)
    {
        string cacheKey = BuildImageByIdCacheKey(imageId);
        if (this.cache.TryGetValue(cacheKey, out Image? cachedImage) && cachedImage is not null)
        {
            return cachedImage;
        }

        ImageDocument? document = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        Image? image = document?.ToDomain();
        if (image is not null)
        {
            this.cache.Set(cacheKey, image, OwnerImagesCacheDuration);
        }

        return image;
    }

    public async Task<IReadOnlyCollection<Image>> GetByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category, CancellationToken cancellationToken)
    {
        string cacheKey = BuildOwnerImagesCacheKey(ownerType, ownerId, category);
        if (this.cache.TryGetValue(cacheKey, out IReadOnlyCollection<Image>? cachedImages) && cachedImages is not null)
        {
            return cachedImages;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, ownerId);

        if (category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, category.Value);
        }

        List<ImageDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<Image> images = documents.Select(static document => document.ToDomain()).ToList();
        this.cache.Set(cacheKey, images, OwnerImagesCacheDuration);
        return images;
    }

    public async Task<IReadOnlyCollection<Image>> GetByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory? category, CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedOwnerIds.Count == 0)
        {
            return Array.Empty<Image>();
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.In(static document => document.OwnerId, normalizedOwnerIds);

        if (category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, category.Value);
        }

        List<ImageDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<Image?> GetByOwnerAndSourceUrlAsync(ImageOwnerType ownerType, string ownerId, string sourceUrl, CancellationToken cancellationToken)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedSourceUrl = string.IsNullOrWhiteSpace(sourceUrl) ? string.Empty : sourceUrl.Trim();
        if (string.IsNullOrWhiteSpace(normalizedOwnerId) || string.IsNullOrWhiteSpace(normalizedSourceUrl))
        {
            return null;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, normalizedOwnerId) &
                                                 builder.Eq(static document => document.SourceUrl, normalizedSourceUrl);

        ImageDocument? document = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyDictionary<string, string>> GetMainImageIdsByOwnersAsync(ImageOwnerType ownerType, IReadOnlyCollection<string> ownerIds, ImageCategory category, bool publishedOnly, CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedOwnerIds.Count == 0)
        {
            return new Dictionary<string, string>(StringComparer.Ordinal);
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.In(static document => document.OwnerId, normalizedOwnerIds) &
                                                 BuildCategoryFilter(builder, category);

        if (publishedOnly)
        {
            filter &= builder.Eq(static document => document.IsPublished, true);
        }

        List<ImageOwnerMainImageProjection> projections = await this.collection.Find(filter)
            .SortByDescending(static document => document.IsCurrent)
            .ThenByDescending(static document => document.CreatedAt)
            .Project(static document => new ImageOwnerMainImageProjection
            {
                Id = document.Id,
                OwnerId = document.OwnerId,
            })
            .ToListAsync(cancellationToken);

        Dictionary<string, string> imageIdsByOwnerId = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (ImageOwnerMainImageProjection projection in projections)
        {
            if (string.IsNullOrWhiteSpace(projection.OwnerId) || string.IsNullOrWhiteSpace(projection.Id))
            {
                continue;
            }

            if (!imageIdsByOwnerId.ContainsKey(projection.OwnerId))
            {
                imageIdsByOwnerId[projection.OwnerId] = projection.Id;
            }
        }

        return imageIdsByOwnerId;
    }

    public async Task<Image?> GetCurrentByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, CancellationToken cancellationToken)
    {
        string cacheKey = BuildCurrentOwnerImageCacheKey(ownerType, ownerId, category);
        if (this.cache.TryGetValue(cacheKey, out Image? cachedImage) && cachedImage is not null)
        {
            return cachedImage;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = BuildOwnerTypeFilter(builder, ownerType) &
                                                 builder.Eq(static document => document.OwnerId, ownerId) &
                                                 BuildCategoryFilter(builder, category) &
                                                 builder.Eq(static document => document.IsCurrent, true);

        ImageDocument? document = await this.collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        Image? image = document?.ToDomain();
        if (image is not null)
        {
            this.cache.Set(cacheKey, image, OwnerImagesCacheDuration);
        }

        return image;
    }

    public async Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken)
    {
        List<LocalizedTextDocument> captions = string.IsNullOrWhiteSpace(request.Description)
            ? new List<LocalizedTextDocument>()
            : new List<LocalizedTextDocument>
            {
                new LocalizedTextDocument
                {
                    LanguageCode = "fr",
                    Value = request.Description,
                },
            };

        ImageDocument document = new ImageDocument
        {
            Id = string.IsNullOrWhiteSpace(request.ImageId) ? Guid.NewGuid().ToString("N") : request.ImageId,
            Category = request.Category,
            Description = request.Description,
            Path = string.IsNullOrWhiteSpace(request.StoragePath) ? $"{request.Category}/{Guid.NewGuid():N}_{request.File.FileName}" : request.StoragePath,
            SizeInBytes = request.SizeInBytes > 0 ? request.SizeInBytes : request.File.Length,
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            IsPublished = true,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            SourceUrl = string.IsNullOrWhiteSpace(request.SourceUrl) ? null : request.SourceUrl.Trim(),
            IsWatermarked = request.WithWatermark,
            Width = request.Width,
            Height = request.Height,
            GeoLocation = request.GeoLocation is null ? null : CommonMongoMappers.ToDocument(new GeoPoint(request.GeoLocation.Latitude, request.GeoLocation.Longitude)),
            ExifMetadata = request.ExifMetadata?.ToDocument(),
            Captions = captions,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        InvalidateReadCache();
        return document.ToDomain();
    }

    public async Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.OwnerType, ownerType)
            .Set(static document => document.OwnerId, ownerId)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        ImageDocument? currentDocument = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        if (currentDocument is null)
        {
            return null;
        }

        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> ownerFilter = BuildOwnerTypeFilter(builder, ownerType) &
                                                     builder.Eq(static document => document.OwnerId, ownerId) &
                                                     BuildCategoryFilter(builder, currentDocument.Category);

        await this.collection.UpdateManyAsync(
            ownerFilter,
            Builders<ImageDocument>.Update
                .Set(static document => document.IsCurrent, false)
                .Set(static document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        FilterDefinition<ImageDocument> targetFilter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> targetUpdate = Builders<ImageDocument>.Update
            .Set(static document => document.OwnerType, ownerType)
            .Set(static document => document.OwnerId, ownerId)
            .Set(static document => document.IsCurrent, true)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(targetFilter, targetUpdate, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.Description, metadata.Description)
            .Set(static document => document.GeoLocation, metadata.GeoLocation is null ? null : CommonMongoMappers.ToDocument(new GeoPoint(metadata.GeoLocation.Latitude, metadata.GeoLocation.Longitude)))
            .Set(static document => document.AltTexts, CommonMongoMappers.ToDocuments(metadata.AltTexts))
            .Set(static document => document.Captions, CommonMongoMappers.ToDocuments(metadata.Captions))
            .Set(static document => document.Credits, CommonMongoMappers.ToDocuments(metadata.Credits))
            .Set(static document => document.TagIds, metadata.TagIds.Distinct(StringComparer.Ordinal).ToList())
            .Set(static document => document.Category, metadata.Category)
            .Set(static document => document.IsPublished, metadata.IsPublished)
            .Set(static document => document.SourceUrl, string.IsNullOrWhiteSpace(metadata.SourceUrl) ? null : metadata.SourceUrl.Trim())
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        if (metadata.OwnerType.HasValue)
        {
            update = update
                .Set(static document => document.OwnerType, metadata.OwnerType.Value)
                .Set(static document => document.OwnerId, string.IsNullOrWhiteSpace(metadata.OwnerId) ? null : metadata.OwnerId.Trim());
        }

        if (metadata.IsCurrent.HasValue)
        {
            update = update.Set(static document => document.IsCurrent, metadata.IsCurrent.Value);
        }

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        InvalidateReadCache();
        return document?.ToDomain();
    }

    public async Task<Image?> MarkWatermarkedAsync(string imageId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(static document => document.IsWatermarked, true)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateReadCache();
        }

        return document?.ToDomain();
    }

    public async Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == imageId, cancellationToken: cancellationToken);
        if (result.DeletedCount > 0)
        {
            InvalidateReadCache();
        }

        return result.DeletedCount > 0;
    }

    public async Task<int> UpdateBulkMetadataAsync(IReadOnlyCollection<string> imageIds, ImageBulkMetadataUpdate metadata, CancellationToken cancellationToken)
    {
        List<string> normalizedImageIds = imageIds
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedImageIds.Count == 0)
        {
            return 0;
        }

        List<UpdateDefinition<ImageDocument>> updates = new List<UpdateDefinition<ImageDocument>>
        {
            Builders<ImageDocument>.Update.Set(static document => document.UpdatedAt, DateTime.UtcNow),
        };

        if (metadata.IsPublished.HasValue)
        {
            updates.Add(Builders<ImageDocument>.Update.Set(static document => document.IsPublished, metadata.IsPublished.Value));
        }

        if (metadata.Category.HasValue)
        {
            updates.Add(Builders<ImageDocument>.Update.Set(static document => document.Category, metadata.Category.Value));
        }

        List<string> tagIdsToAdd = metadata.AddTagIds?
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? new List<string>();

        if (tagIdsToAdd.Count > 0)
        {
            updates.Add(Builders<ImageDocument>.Update.AddToSetEach(static document => document.TagIds, tagIdsToAdd));
        }

        List<string> tagIdsToRemove = metadata.RemoveTagIds?
            .Where(static tagId => !string.IsNullOrWhiteSpace(tagId))
            .Distinct(StringComparer.Ordinal)
            .ToList() ?? new List<string>();

        if (tagIdsToRemove.Count > 0)
        {
            updates.Add(Builders<ImageDocument>.Update.PullAll(static document => document.TagIds, tagIdsToRemove));
        }

        if (updates.Count <= 1)
        {
            return 0;
        }

        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.In(static document => document.Id, normalizedImageIds);
        UpdateResult result = await this.collection.UpdateManyAsync(filter, Builders<ImageDocument>.Update.Combine(updates), cancellationToken: cancellationToken);
        if (result.ModifiedCount > 0)
        {
            InvalidateReadCache();
        }

        return checked((int)result.ModifiedCount);
    }

    private static string BuildImageByIdCacheKey(string imageId)
    {
        string normalizedImageId = string.IsNullOrWhiteSpace(imageId) ? string.Empty : imageId.Trim();
        return $"images:by-id:{GetCacheVersion()}:{normalizedImageId}";
    }

    private static string BuildOwnerImagesCacheKey(ImageOwnerType ownerType, string ownerId, ImageCategory? category)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        string normalizedCategory = category.HasValue ? category.Value.ToString() : "all";
        return $"images:owner:{GetCacheVersion()}:{ownerType}:{normalizedOwnerId}:{normalizedCategory}";
    }

    private static string BuildCurrentOwnerImageCacheKey(ImageOwnerType ownerType, string ownerId, ImageCategory category)
    {
        string normalizedOwnerId = string.IsNullOrWhiteSpace(ownerId) ? string.Empty : ownerId.Trim();
        return $"images:current:{GetCacheVersion()}:{ownerType}:{normalizedOwnerId}:{category}";
    }

    private static long GetCacheVersion()
    {
        return Volatile.Read(ref cacheVersion);
    }

    private static void InvalidateReadCache()
    {
        Interlocked.Increment(ref cacheVersion);
    }

    private static FilterDefinition<ImageDocument> BuildFilter(ImageSearchCriteria criteria)
    {
        FilterDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Filter;
        FilterDefinition<ImageDocument> filter = builder.Empty;

        if (criteria.Category.HasValue)
        {
            filter &= BuildCategoryFilter(builder, criteria.Category.Value);
        }

        if (criteria.OwnerType.HasValue)
        {
            filter &= BuildOwnerTypeFilter(builder, criteria.OwnerType.Value);
        }

        if (!string.IsNullOrWhiteSpace(criteria.OwnerId))
        {
            filter &= builder.Eq(static document => document.OwnerId, criteria.OwnerId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(criteria.TagId))
        {
            filter &= builder.AnyEq(static document => document.TagIds, criteria.TagId.Trim());
        }

        if (criteria.IsPublished.HasValue)
        {
            filter &= builder.Eq(static document => document.IsPublished, criteria.IsPublished.Value);
        }

        if (criteria.HasOwner.HasValue)
        {
            FilterDefinition<ImageDocument> hasOwnerFilter = builder.Ne(static document => document.OwnerType, ImageOwnerType.None) &
                                                             builder.Ne(static document => document.OwnerId, null) &
                                                             builder.Ne(static document => document.OwnerId, string.Empty);
            filter &= criteria.HasOwner.Value ? hasOwnerFilter : builder.Not(hasOwnerFilter);
        }

        if (criteria.HasGeoLocation.HasValue)
        {
            FilterDefinition<ImageDocument> hasGeoLocationFilter = builder.Ne(static document => document.GeoLocation, null);
            filter &= criteria.HasGeoLocation.Value ? hasGeoLocationFilter : builder.Not(hasGeoLocationFilter);
        }

        if (!string.IsNullOrWhiteSpace(criteria.Search))
        {
            string escapedSearch = Regex.Escape(criteria.Search.Trim());
            BsonRegularExpression regex = new BsonRegularExpression(escapedSearch, "i");
            filter &= builder.Or(
                builder.Regex(static document => document.Id, regex),
                builder.Regex(static document => document.OriginalFileName, regex),
                builder.Regex(static document => document.Path, regex),
                builder.Regex(static document => document.Description, regex),
                builder.Regex(static document => document.ContentType, regex),
                builder.Regex(static document => document.OwnerId, regex),
                builder.Regex("altTexts.value", regex),
                builder.Regex("captions.value", regex),
                builder.Regex("credits.value", regex));
        }

        return filter;
    }

    private static FilterDefinition<ImageDocument> BuildOwnerTypeFilter(FilterDefinitionBuilder<ImageDocument> builder, ImageOwnerType ownerType)
    {
        FilterDefinition<ImageDocument> currentFilter = builder.Eq(static document => document.OwnerType, ownerType);
        return ownerType == ImageOwnerType.ParkItem
            ? builder.Or(currentFilter, builder.Eq("ownerType", "Attraction"))
            : currentFilter;
    }

    private static FilterDefinition<ImageDocument> BuildCategoryFilter(FilterDefinitionBuilder<ImageDocument> builder, ImageCategory category)
    {
        FilterDefinition<ImageDocument> currentFilter = builder.Eq(static document => document.Category, category);
        return category == ImageCategory.ParkItem
            ? builder.Or(currentFilter, builder.Eq("category", "Attraction"))
            : currentFilter;
    }

    private static SortDefinition<ImageDocument> BuildSort(ImageSearchCriteria criteria)
    {
        bool descending = !string.Equals(criteria.SortDirection, "asc", StringComparison.OrdinalIgnoreCase);
        SortDefinitionBuilder<ImageDocument> builder = Builders<ImageDocument>.Sort;

        return (criteria.SortBy?.Trim().ToLowerInvariant(), descending) switch
        {
            ("filename", true) => builder.Descending(static document => document.OriginalFileName).Descending(static document => document.CreatedAt),
            ("filename", false) => builder.Ascending(static document => document.OriginalFileName).Ascending(static document => document.CreatedAt),
            ("size", true) => builder.Descending(static document => document.SizeInBytes).Descending(static document => document.CreatedAt),
            ("size", false) => builder.Ascending(static document => document.SizeInBytes).Ascending(static document => document.CreatedAt),
            ("dimensions", true) => builder.Descending(static document => document.Width).Descending(static document => document.Height),
            ("dimensions", false) => builder.Ascending(static document => document.Width).Ascending(static document => document.Height),
            ("updated", true) => builder.Descending(static document => document.UpdatedAt),
            ("updated", false) => builder.Ascending(static document => document.UpdatedAt),
            ("created", false) => builder.Ascending(static document => document.CreatedAt),
            _ => builder.Descending(static document => document.CreatedAt),
        };
    }

    private sealed class ImageOwnerMainImageProjection
    {
        public string? Id { get; init; }

        public string? OwnerId { get; init; }
    }
}

/// <summary>
/// Repository Mongo des tags d'images.
/// </summary>
public sealed class ImageTagRepository : IImageTagRepository
{
    private static readonly TimeSpan TagCacheDuration = TimeSpan.FromMinutes(30);
    private static long tagCacheVersion;
    private readonly IMongoCollection<ImageTagDocument> collection;
    private readonly IMemoryCache cache;

    public ImageTagRepository(IMongoDatabase database, MongoDbSettings settings, IMemoryCache cache)
    {
        this.collection = database.GetCollection<ImageTagDocument>(settings.ImageTagsCollectionName);
        this.cache = cache;
    }

    public async Task<IReadOnlyCollection<ImageTag>> GetAllAsync(CancellationToken cancellationToken)
    {
        string cacheKey = BuildAllTagsCacheKey();
        if (this.cache.TryGetValue(cacheKey, out IReadOnlyCollection<ImageTag>? cachedTags) && cachedTags is not null)
        {
            return cachedTags;
        }

        List<ImageTagDocument> documents = await this.collection.Find(Builders<ImageTagDocument>.Filter.Empty)
            .SortBy(static document => document.Slug)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<ImageTag> tags = documents.Select(static document => document.ToDomain()).ToList();
        this.cache.Set(cacheKey, tags, TagCacheDuration);
        return tags;
    }

    public async Task<ImageTag?> GetByIdAsync(string tagId, CancellationToken cancellationToken)
    {
        ImageTagDocument? document = await this.collection.Find(document => document.Id == tagId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ImageTag?> GetBySlugAsync(string slug, CancellationToken cancellationToken)
    {
        ImageTagDocument? document = await this.collection.Find(document => document.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ImageTag> CreateAsync(ImageTagWriteModel tag, CancellationToken cancellationToken)
    {
        ImageTagDocument document = new ImageTagDocument
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

    public async Task<ImageTag?> UpdateAsync(string tagId, ImageTagWriteModel tag, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageTagDocument> filter = Builders<ImageTagDocument>.Filter.Eq(static document => document.Id, tagId);
        UpdateDefinition<ImageTagDocument> update = Builders<ImageTagDocument>.Update
            .Set(static document => document.Slug, tag.Slug)
            .Set(static document => document.Labels, CommonMongoMappers.ToDocuments(tag.Labels))
            .Set(static document => document.Descriptions, CommonMongoMappers.ToDocuments(tag.Descriptions))
            .Set(static document => document.IsActive, tag.IsActive)
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageTagDocument> options = new FindOneAndUpdateOptions<ImageTagDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageTagDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is not null)
        {
            InvalidateTagCache();
        }

        return document?.ToDomain();
    }

    private static string BuildAllTagsCacheKey()
    {
        return $"image-tags:all:{Volatile.Read(ref tagCacheVersion)}";
    }

    private static void InvalidateTagCache()
    {
        Interlocked.Increment(ref tagCacheVersion);
    }
}
