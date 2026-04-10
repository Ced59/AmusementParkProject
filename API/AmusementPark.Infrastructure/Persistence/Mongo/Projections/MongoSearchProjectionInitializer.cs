using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
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
        await this.RebuildAsync(cancellationToken);
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
                Builders<SearchItemDocument>.IndexKeys.Geo2DSphere(item => item.Location)),
            new CreateIndexModel<SearchItemDocument>(
                Builders<SearchItemDocument>.IndexKeys.Ascending(item => item.Category).Ascending(item => item.IsVisible).Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_search_items_category_visibility_updated" }),
            new CreateIndexModel<SearchItemDocument>(
                textIndexKeys,
                new CreateIndexOptions { DefaultLanguage = "french", Name = "Idx_SearchItem_Text" }),
        };

        await searchCollection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }

    private async Task RebuildAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<ParkDocument> parksCollection = this.database.GetCollection<ParkDocument>(this.settings.ParksCollectionName);
        IMongoCollection<ParkItemDocument> parkItemsCollection = this.database.GetCollection<ParkItemDocument>(this.settings.ParkItemsCollectionName);
        IMongoCollection<ParkFounderDocument> parkFoundersCollection = this.database.GetCollection<ParkFounderDocument>(this.settings.ParkFoundersCollectionName);
        IMongoCollection<ParkOperatorDocument> parkOperatorsCollection = this.database.GetCollection<ParkOperatorDocument>(this.settings.ParkOperatorsCollectionName);
        IMongoCollection<AttractionManufacturerDocument> attractionManufacturersCollection = this.database.GetCollection<AttractionManufacturerDocument>(this.settings.AttractionManufacturersCollectionName);

        List<string> parkIds = await parksCollection.Find(Builders<ParkDocument>.Filter.Empty)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        List<string> parkItemIds = await parkItemsCollection.Find(Builders<ParkItemDocument>.Filter.Empty)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        List<string> parkFounderIds = await parkFoundersCollection.Find(Builders<ParkFounderDocument>.Filter.Empty)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        List<string> parkOperatorIds = await parkOperatorsCollection.Find(Builders<ParkOperatorDocument>.Filter.Empty)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        List<string> attractionManufacturerIds = await attractionManufacturersCollection.Find(Builders<AttractionManufacturerDocument>.Filter.Empty)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        await this.UpsertAllAsync(parkIds, SearchProjectionResourceTypes.Parks, cancellationToken);
        await this.UpsertAllAsync(parkItemIds, SearchProjectionResourceTypes.ParkItems, cancellationToken);
        await this.UpsertAllAsync(parkFounderIds, SearchProjectionResourceTypes.Founders, cancellationToken);
        await this.UpsertAllAsync(parkOperatorIds, SearchProjectionResourceTypes.Operators, cancellationToken);
        await this.UpsertAllAsync(attractionManufacturerIds, SearchProjectionResourceTypes.Manufacturers, cancellationToken);
    }

    private async Task UpsertAllAsync(IEnumerable<string> ids, string resourceType, CancellationToken cancellationToken)
    {
        foreach (string id in ids)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                continue;
            }

            await this.searchProjectionWriter.UpsertAsync(resourceType, id, cancellationToken);
        }
    }
}
