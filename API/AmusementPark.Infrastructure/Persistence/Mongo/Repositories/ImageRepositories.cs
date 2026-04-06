using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Configuration.Mongo;
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

    public async Task<Image?> GetByIdAsync(string imageId, CancellationToken cancellationToken)
    {
        ImageDocument? document = await this.collection.Find(document => document.Id == imageId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<Image> CreateAsync(ImageUploadRequest request, CancellationToken cancellationToken)
    {
        ImageDocument document = new ImageDocument
        {
            Id = Guid.NewGuid().ToString(),
            Category = request.Category,
            Description = request.Description,
            Path = $"{request.OwnerType}/{Guid.NewGuid():N}_{request.File.FileName}",
            SizeInBytes = request.File.Length,
            OwnerType = request.OwnerType,
            OwnerId = request.OwnerId,
            IsPublished = true,
            OriginalFileName = request.File.FileName,
            ContentType = request.File.ContentType,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Image?> LinkAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(document => document.OwnerType, ownerType)
            .Set(document => document.OwnerId, ownerId)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }

    public async Task<Image?> SetCurrentAsync(string imageId, ImageOwnerType ownerType, string ownerId, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> ownerFilter = Builders<ImageDocument>.Filter.Eq(document => document.OwnerType, ownerType) &
                                                     Builders<ImageDocument>.Filter.Eq(document => document.OwnerId, ownerId);

        await this.collection.UpdateManyAsync(
            ownerFilter,
            Builders<ImageDocument>.Update
                .Set(document => document.IsCurrent, false)
                .Set(document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        FilterDefinition<ImageDocument> targetFilter = Builders<ImageDocument>.Filter.Eq(document => document.Id, imageId);
        UpdateDefinition<ImageDocument> targetUpdate = Builders<ImageDocument>.Update
            .Set(document => document.OwnerType, ownerType)
            .Set(document => document.OwnerId, ownerId)
            .Set(document => document.IsCurrent, true)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageDocument> options = new FindOneAndUpdateOptions<ImageDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageDocument? document = await this.collection.FindOneAndUpdateAsync(targetFilter, targetUpdate, options, cancellationToken);
        return document?.ToDomain();
    }

    public async Task<Image?> UpdateMetadataAsync(string imageId, ImageMetadataUpdate metadata, CancellationToken cancellationToken)
    {
        FilterDefinition<ImageDocument> filter = Builders<ImageDocument>.Filter.Eq(document => document.Id, imageId);
        UpdateDefinition<ImageDocument> update = Builders<ImageDocument>.Update
            .Set(document => document.Description, metadata.Description)
            .Set(document => document.AltTexts, CommonMongoMappers.ToDocuments(metadata.AltTexts))
            .Set(document => document.Captions, CommonMongoMappers.ToDocuments(metadata.Captions))
            .Set(document => document.Credits, CommonMongoMappers.ToDocuments(metadata.Credits))
            .Set(document => document.TagIds, metadata.TagIds.ToList())
            .Set(document => document.Category, metadata.Category)
            .Set(document => document.IsPublished, metadata.IsPublished)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

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
            .SortBy(document => document.Slug)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<ImageTag> CreateAsync(ImageTagWriteModel tag, CancellationToken cancellationToken)
    {
        ImageTagDocument document = new ImageTagDocument
        {
            Id = Guid.NewGuid().ToString(),
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
        FilterDefinition<ImageTagDocument> filter = Builders<ImageTagDocument>.Filter.Eq(document => document.Id, tagId);
        UpdateDefinition<ImageTagDocument> update = Builders<ImageTagDocument>.Update
            .Set(document => document.Slug, tag.Slug)
            .Set(document => document.Labels, CommonMongoMappers.ToDocuments(tag.Labels))
            .Set(document => document.Descriptions, CommonMongoMappers.ToDocuments(tag.Descriptions))
            .Set(document => document.IsActive, tag.IsActive)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ImageTagDocument> options = new FindOneAndUpdateOptions<ImageTagDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ImageTagDocument? document = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return document?.ToDomain();
    }
}
