using System.Linq.Expressions;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Projections;

/// <summary>
/// Initialise la projection de recherche Mongo et reconstruit l'index technique à partir des données métier.
/// </summary>
public sealed class MongoSearchProjectionInitializer
{
    private readonly IMongoDatabase database;
    private readonly MongoDbSettings settings;
    private readonly ISearchProjectionWriter searchProjectionWriter;

    public MongoSearchProjectionInitializer(IMongoDatabase database, MongoDbSettings settings, ISearchProjectionWriter searchProjectionWriter)
    {
        this.database = database;
        this.settings = settings;
        this.searchProjectionWriter = searchProjectionWriter;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        await this.EnsureCollectionAndIndexesAsync(cancellationToken);

        if (this.settings.RebuildSearchProjectionOnStartup || await this.IsSearchProjectionEmptyAsync(cancellationToken))
        {
            await this.RebuildAsync(cancellationToken);
            return;
        }

        await this.BackfillLocalizedDescriptionsAsync(cancellationToken);
    }

    private async Task EnsureCollectionAndIndexesAsync(CancellationToken cancellationToken)
    {
        BsonDocument filter = new BsonDocument("name", this.settings.SearchItemCollectionName);
        using IAsyncCursor<BsonDocument> cursor = await this.database.ListCollectionsAsync(new ListCollectionsOptions { Filter = filter }, cancellationToken);
        bool exists = await cursor.AnyAsync(cancellationToken);
        if (!exists)
        {
            await this.database.CreateCollectionAsync(this.settings.SearchItemCollectionName, cancellationToken: cancellationToken);
        }

        IMongoCollection<SearchItemDocument> searchCollection = this.database.GetCollection<SearchItemDocument>(this.settings.SearchItemCollectionName);

        IndexKeysDefinition<SearchItemDocument> textIndexKeys = Builders<SearchItemDocument>.IndexKeys
            .Text(item => item.Title)
            .Text(item => item.Subtitle)
            .Text(item => item.Description)
            .Text(item => item.Keywords);

        List<CreateIndexModel<SearchItemDocument>> indexes = new List<CreateIndexModel<SearchItemDocument>>
        {
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.OriginalId),
                new CreateIndexOptions { Name = "idx_search_items_original_id_unique", Unique = true }),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Geo2DSphere(item => item.Location),
                new CreateIndexOptions { Name = "location_2dsphere" }),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_search_items_category_visibility_updated" }),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.IsVisible).Descending(item => item.CompositeScore).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_search_items_visibility_score_updated" }),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.ResourceType).Ascending(item => item.IsVisible).Descending(item => item.CompositeScore).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_search_items_resource_type_visibility_score_updated" }),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.ParentParkId).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_search_items_parent_park_visibility_updated" }),
            new CreateIndexModel<SearchItemDocument>(
                textIndexKeys,
                new CreateIndexOptions { DefaultLanguage = "french", Name = "Idx_SearchItem_Text" }),
        };

        await searchCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task<bool> IsSearchProjectionEmptyAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SearchItemDocument> searchCollection = this.database.GetCollection<SearchItemDocument>(this.settings.SearchItemCollectionName);
        long count = await searchCollection.CountDocumentsAsync(
            Builders<SearchItemDocument>.Filter.Empty,
            new CountOptions { Limit = 1 },
            cancellationToken);
        return count == 0;
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDocument> parksCollection = this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        IMongoCollection<ParkItemDocument> parkItemsCollection = this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName);
        IMongoCollection<StandaloneAttractionDocument> standaloneAttractionsCollection = this.database.GetCollection<StandaloneAttractionDocument>(this.settings.StandaloneAttractionsCollectionName);
        IMongoCollection<ParkFounderDocument> parkFoundersCollection = this.database.GetCollection<ParkFounderDocument>(this.settings.ParkFoundersCollectionName);
        IMongoCollection<ParkOperatorDocument> parkOperatorsCollection = this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName);
        IMongoCollection<AttractionManufacturerDocument> attractionManufacturerCollection = this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName);

        await this.UpsertAllAsync(parksCollection, document => document.Id, SearchProjectionResourceTypes.Parks, cancellationToken);
        await this.UpsertAllAsync(parkItemsCollection, document => document.Id, SearchProjectionResourceTypes.ParkItems, cancellationToken);
        await this.UpsertAllAsync(standaloneAttractionsCollection, document => document.Id, SearchProjectionResourceTypes.StandaloneAttractions, cancellationToken);
        await this.UpsertAllAsync(parkFoundersCollection, document => document.Id, SearchProjectionResourceTypes.Founders, cancellationToken);
        await this.UpsertAllAsync(parkOperatorsCollection, document => document.Id, SearchProjectionResourceTypes.Operators, cancellationToken);
        await this.UpsertAllAsync(attractionManufacturerCollection, document => document.Id, SearchProjectionResourceTypes.Manufacturers, cancellationToken);
    }

    private async Task BackfillLocalizedDescriptionsAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SearchItemDocument> searchCollection = this.database.GetCollection<SearchItemDocument>(this.settings.SearchItemCollectionName);
        FilterDefinition<SearchItemDocument> missingLocalizedDescriptions = Builders<SearchItemDocument>.Filter.Exists("localizedDescriptions", false);
        int batchSize = Math.Max(1, this.settings.SearchProjectionRebuildBatchSize);
        int delayMilliseconds = Math.Max(0, this.settings.SearchProjectionRebuildBatchDelayMilliseconds);
        Dictionary<string, List<string>> batchesByResourceType = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        using IAsyncCursor<SearchItemDocument> cursor = await searchCollection
            .Find(missingLocalizedDescriptions)
            .Project(document => new SearchItemDocument
            {
                OriginalId = document.OriginalId,
                ResourceType = document.ResourceType,
                Category = document.Category,
            })
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (SearchItemDocument document in cursor.Current)
            {
                if (!TryResolveProjectionResource(document, out string resourceType, out string resourceId))
                {
                    continue;
                }

                if (!batchesByResourceType.TryGetValue(resourceType, out List<string>? batch))
                {
                    batch = new List<string>(batchSize);
                    batchesByResourceType[resourceType] = batch;
                }

                batch.Add(resourceId);
                if (batch.Count >= batchSize)
                {
                    await this.FlushBatchAsync(resourceType, batch, delayMilliseconds, cancellationToken);
                }
            }
        }

        foreach (KeyValuePair<string, List<string>> batchByResourceType in batchesByResourceType)
        {
            if (batchByResourceType.Value.Count > 0)
            {
                await this.FlushBatchAsync(batchByResourceType.Key, batchByResourceType.Value, delayMilliseconds, cancellationToken);
            }
        }
    }

    private async Task UpsertAllAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        Expression<Func<TDocument, string>> idProjection,
        string resourceType,
        CancellationToken cancellationToken)
    {
        int batchSize = Math.Max(1, this.settings.SearchProjectionRebuildBatchSize);
        int delayMilliseconds = Math.Max(0, this.settings.SearchProjectionRebuildBatchDelayMilliseconds);
        List<string> batch = new List<string>(batchSize);

        using IAsyncCursor<string> cursor = await collection
            .Find(Builders<TDocument>.Filter.Empty)
            .Project(idProjection)
            .ToCursorAsync(cancellationToken);

        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (string id in cursor.Current)
            {
                if (string.IsNullOrWhiteSpace(id))
                {
                    continue;
                }

                batch.Add(id.Trim());
                if (batch.Count >= batchSize)
                {
                    await this.FlushBatchAsync(resourceType, batch, delayMilliseconds, cancellationToken);
                }
            }
        }

        if (batch.Count > 0)
        {
            await this.FlushBatchAsync(resourceType, batch, delayMilliseconds, cancellationToken);
        }
    }

    private async Task FlushBatchAsync(
        string resourceType,
        List<string> batch,
        int delayMilliseconds,
        CancellationToken cancellationToken)
    {
        List<string> ids = batch
            .Distinct(StringComparer.Ordinal)
            .ToList();
        batch.Clear();

        if (ids.Count == 0)
        {
            return;
        }

        await this.searchProjectionWriter.UpsertManyAsync(resourceType, ids, cancellationToken);

        if (delayMilliseconds > 0)
        {
            await Task.Delay(delayMilliseconds, cancellationToken);
        }
    }

    private static bool TryResolveProjectionResource(SearchItemDocument document, out string resourceType, out string resourceId)
    {
        if (TryResolveOriginalId(document.OriginalId, "parkItem_", SearchProjectionResourceTypes.ParkItems, out resourceType, out resourceId))
        {
            return true;
        }

        if (TryResolveOriginalId(document.OriginalId, "standaloneAttraction_", SearchProjectionResourceTypes.StandaloneAttractions, out resourceType, out resourceId))
        {
            return true;
        }

        if (TryResolveOriginalId(document.OriginalId, "park_", SearchProjectionResourceTypes.Parks, out resourceType, out resourceId))
        {
            return true;
        }

        if (TryResolveOriginalId(document.OriginalId, "operator_", SearchProjectionResourceTypes.Operators, out resourceType, out resourceId))
        {
            return true;
        }

        if (TryResolveOriginalId(document.OriginalId, "manufacturer_", SearchProjectionResourceTypes.Manufacturers, out resourceType, out resourceId))
        {
            return true;
        }

        if (TryResolveOriginalId(document.OriginalId, "founder_", SearchProjectionResourceTypes.Founders, out resourceType, out resourceId))
        {
            return true;
        }

        resourceType = string.Empty;
        resourceId = string.Empty;
        return false;
    }

    private static bool TryResolveOriginalId(string? originalId, string prefix, string targetResourceType, out string resourceType, out string resourceId)
    {
        if (!string.IsNullOrWhiteSpace(originalId) && originalId.StartsWith(prefix, StringComparison.Ordinal))
        {
            resourceType = targetResourceType;
            resourceId = originalId[prefix.Length..].Trim();
            return !string.IsNullOrWhiteSpace(resourceId);
        }

        resourceType = string.Empty;
        resourceId = string.Empty;
        return false;
    }
}
