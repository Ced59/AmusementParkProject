using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Geo;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des images.
/// </summary>
public sealed class ImageRepository : IImageRepository
{
    private readonly IMongoCollection<ImageDocument> collection;

    public ImageRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ImageDocument>(settings.ImagesCollectionName);
    }

    public async Task<IReadOnlyCollection<Image>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ImageDocument> documents = await this.collection.Find(Builders<ImageDocument>.Filter.Empty)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken)
    {
        ImageDocument? document = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<Image>> GetByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory? category, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(static document => document.OwnerType, ownerType) &
                                                 Builders<ImageDocument>.Filter.Eq(static document => document.OwnerId, ownerId);

        if (category.HasValue)
        {
            filter &= Builders<ImageDocument>.Filter.Eq(static document => document.Category, category.Value);
        }

        List<ImageDocument> documents = await this.collection.Find(filter)
            .SortByDescending(static document => document.CreatedAt)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<Image?> GetCurrentByOwnerAsync(ImageOwnerType ownerType, string ownerId, ImageCategory category, CancellationToken cancellationToken)
    {
        ImageDocument? document = await this.collection.Find(document =>
                document.OwnerType == ownerType &&
                document.OwnerId == ownerId &&
                document.Category == category &&
                document.IsCurrent)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
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
            Width = request.Width,
            Height = request.Height,
            GeoLocation = request.GeoLocation is null ? null : CommonMongoMappers.ToDocument(new GeoPoint(request.GeoLocation.Latitude, request.GeoLocation.Longitude)),
            ExifMetadata = request.ExifMetadata?.ToDocument(),
            Captions = captions,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
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

        FilterDefinition<ImageDocument> ownerFilter = Builders<ImageDocument>.Filter.Eq(static document => document.OwnerType, ownerType) &
                                                     Builders<ImageDocument>.Filter.Eq(static document => document.OwnerId, ownerId) &
                                                     Builders<ImageDocument>.Filter.Eq(static document => document.Category, currentDocument.Category);

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
            .Set(static document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> DeleteAsync(string imageId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == imageId, cancellationToken: cancellationToken);
        return result.DeletedCount > 0;
    }
}

/// <summary>
/// Repository Mongo des tags d'images.
/// </summary>
public sealed class ImageTagRepository : IImageTagRepository
{
    private readonly IMongoCollection<ImageTagDocument> collection;

    public ImageTagRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ImageTagDocument>(settings.ImageTagsCollectionName);
    }

    public async Task<IReadOnlyCollection<ImageTag>> GetAllAsync(CancellationToken cancellationToken)
    {
        List<ImageTagDocument> documents = await this.collection.Find(Builders<ImageTagDocument>.Filter.Empty)
            .SortBy(static document => document.Slug)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
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
        return document?.ToDomain();
    }
}
