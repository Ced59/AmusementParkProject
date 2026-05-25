using AmusementPark.Application.Features.AttractionAccessConditionTypes;
using AmusementPark.Application.Features.AttractionAccessConditionTypes.Contracts;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeAttractionAccessConditionTypesIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionAccessConditionTypeDefinitionDocument> collection = this.database.GetCollection<AttractionAccessConditionTypeDefinitionDocument>(this.settings.AttractionAccessConditionTypesCollectionName);
        List<CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>> indexes = new List<CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>>
        {
            new CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>(
                Builders<AttractionAccessConditionTypeDefinitionDocument>.IndexKeys.Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_access_condition_types_key_unique", Unique = true }),
            new CreateIndexModel<AttractionAccessConditionTypeDefinitionDocument>(
                Builders<AttractionAccessConditionTypeDefinitionDocument>.IndexKeys.Ascending(item => item.IsActive).Ascending(item => item.SortOrder).Ascending(item => item.Key),
                new CreateIndexOptions { Name = "idx_access_condition_types_active_sort" }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task SeedSystemAttractionAccessConditionTypesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<AttractionAccessConditionTypeDefinitionDocument> collection = this.database.GetCollection<AttractionAccessConditionTypeDefinitionDocument>(this.settings.AttractionAccessConditionTypesCollectionName);

        foreach (AttractionAccessConditionTypeDefinitionWriteModel definition in AttractionAccessConditionTypeDefaultCatalog.BuildSystemDefinitions())
        {
            string key = AttractionAccessConditionTypeKeyNormalizer.Normalize(definition.Key);
            AttractionAccessConditionTypeDefinitionDocument? existing = await collection
                .Find(item => item.Key == key)
                .FirstOrDefaultAsync(cancellationToken);

            AttractionAccessConditionTypeDefinitionDocument document = existing ?? new AttractionAccessConditionTypeDefinitionDocument
            {
                Id = Guid.NewGuid().ToString("N"),
                Key = key,
                CreatedAt = DateTime.UtcNow,
                Labels = CommonMongoMappers.ToDocuments(definition.Labels),
                Descriptions = CommonMongoMappers.ToDocuments(definition.Descriptions),
            };

            document.LegacyType = definition.LegacyType;
            document.IsSystem = true;
            document.IsActive = true;
            if (document.Labels.Count == 0)
            {
                document.Labels = CommonMongoMappers.ToDocuments(definition.Labels);
            }

            if (document.Descriptions.Count == 0)
            {
                document.Descriptions = CommonMongoMappers.ToDocuments(definition.Descriptions);
            }

            document.SortOrder = document.SortOrder <= 0 ? definition.SortOrder : document.SortOrder;
            document.UpdatedAt = DateTime.UtcNow;

            await collection.ReplaceOneAsync(
                item => item.Key == key,
                document,
                new ReplaceOptions { IsUpsert = true },
                cancellationToken);
        }
    }
}
