using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo du catalogue des types de conditions d'accès.
/// </summary>
public sealed class AttractionAccessConditionTypeDefinitionRepository : IAttractionAccessConditionTypeDefinitionRepository
{
    private readonly IMongoCollection<AttractionAccessConditionTypeDefinitionDocument> collection;

    public AttractionAccessConditionTypeDefinitionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<AttractionAccessConditionTypeDefinitionDocument>(settings.AttractionAccessConditionTypesCollectionName);
    }

    public async Task<IReadOnlyCollection<AttractionAccessConditionTypeDefinition>> GetAllAsync(bool includeInactive, CancellationToken cancellationToken)
    {
        FilterDefinition<AttractionAccessConditionTypeDefinitionDocument> filter = includeInactive
            ? Builders<AttractionAccessConditionTypeDefinitionDocument>.Filter.Empty
            : Builders<AttractionAccessConditionTypeDefinitionDocument>.Filter.Eq(static document => document.IsActive, true);

        List<AttractionAccessConditionTypeDefinitionDocument> documents = await this.collection
            .Find(filter)
            .SortBy(static document => document.SortOrder)
            .ThenBy(static document => document.Key)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<AttractionAccessConditionTypeDefinition?> GetByKeyAsync(string key, CancellationToken cancellationToken)
    {
        string normalizedKey = AttractionAccessConditionTypeKeyNormalizer.Normalize(key);
        AttractionAccessConditionTypeDefinitionDocument? document = await this.collection
            .Find(item => item.Key == normalizedKey)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<AttractionAccessConditionTypeDefinition> UpsertAsync(AttractionAccessConditionTypeDefinitionWriteModel model, CancellationToken cancellationToken)
    {
        string normalizedKey = AttractionAccessConditionTypeKeyNormalizer.Normalize(model.Key);
        AttractionAccessConditionTypeDefinitionDocument? existing = await this.collection
            .Find(item => item.Key == normalizedKey)
            .FirstOrDefaultAsync(cancellationToken);

        AttractionAccessConditionTypeDefinitionDocument document = existing ?? new AttractionAccessConditionTypeDefinitionDocument
        {
            Id = Guid.NewGuid().ToString("N"),
            Key = normalizedKey,
            CreatedAt = DateTime.UtcNow,
        };

        document.LegacyType = model.LegacyType;
        document.IsSystem = existing?.IsSystem == true || model.IsSystem;
        document.IsActive = model.IsActive;
        document.Labels = CommonMongoMappers.ToDocuments(model.Labels);
        document.Descriptions = CommonMongoMappers.ToDocuments(model.Descriptions);
        document.SortOrder = model.SortOrder;
        document.UpdatedAt = DateTime.UtcNow;

        await this.collection.ReplaceOneAsync(
            item => item.Key == normalizedKey,
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        return document.ToDomain();
    }
}
