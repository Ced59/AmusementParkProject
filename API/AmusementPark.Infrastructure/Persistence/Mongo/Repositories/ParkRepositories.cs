using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;
using MongoDB.Driver.GeoJsonObjectModel;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo des parcs.
/// </summary>
public sealed class ParkRepository : IParkRepository
{
    private readonly IMongoCollection<ParkDocument> collection;

    public ParkRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
    }

    public async Task<Park?> GetByIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId);

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        ParkDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = includeHidden
            ? Builders<ParkDocument>.Filter.Empty
            : Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
    {
        string escapedName = Regex.Escape(name.Trim());
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Regex(
            document => document.Name,
            new BsonRegularExpression(escapedName, "i"));

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, CancellationToken cancellationToken)
    {
        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
            new GeoJson2DGeographicCoordinates(longitude, latitude));

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.NearSphere(
            document => document.Location,
            center,
            maxDistance: radiusInKilometers * 1000d);

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<Park> CreateAsync(Park park, CancellationToken cancellationToken)
    {
        ParkDocument document = park.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken)
    {
        ParkDocument document = park.ToDocument();
        document.Id = parkId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == parkId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.Id, parkId);
        UpdateDefinition<ParkDocument> update = Builders<ParkDocument>.Update
            .Set(document => document.IsVisible, isVisible)
            .Set(document => document.UpdatedAt, DateTime.UtcNow);

        FindOneAndUpdateOptions<ParkDocument> options = new FindOneAndUpdateOptions<ParkDocument>
        {
            ReturnDocument = ReturnDocument.After,
        };

        ParkDocument? updated = await this.collection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        return updated?.ToDomain();
    }
}

/// <summary>
/// Repository Mongo des zones.
/// </summary>
public sealed class ParkZoneRepository : IParkZoneRepository
{
    private readonly IMongoCollection<ParkZoneDocument> zonesCollection;
    private readonly IMongoCollection<ParkItemDocument> itemsCollection;

    public ParkZoneRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.zonesCollection = database.GetCollection<ParkZoneDocument>(settings.ParkZonesCollectionName);
        this.itemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<IReadOnlyCollection<ParkZone>> GetByParkIdAsync(string parkId, CancellationToken cancellationToken)
    {
        List<ParkZoneDocument> documents = await this.zonesCollection.Find(document => document.ParkId == parkId)
            .SortBy(document => document.SortOrder)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<ParkZone?> GetByIdAsync(string zoneId, CancellationToken cancellationToken)
    {
        ParkZoneDocument? document = await this.zonesCollection.Find(document => document.Id == zoneId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ParkZone> CreateAsync(ParkZone zone, CancellationToken cancellationToken)
    {
        ParkZoneDocument document = zone.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.zonesCollection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<ParkZone?> UpdateAsync(string zoneId, ParkZone zone, CancellationToken cancellationToken)
    {
        ParkZoneDocument document = zone.ToDocument();
        document.Id = zoneId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.zonesCollection.ReplaceOneAsync(
            existing => existing.Id == zoneId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<bool> DeleteAsync(string zoneId, CancellationToken cancellationToken)
    {
        DeleteResult deleteZoneResult = await this.zonesCollection.DeleteOneAsync(
            document => document.Id == zoneId,
            cancellationToken: cancellationToken);

        await this.itemsCollection.UpdateManyAsync(
            document => document.ZoneId == zoneId,
            Builders<ParkItemDocument>.Update
                .Set(document => document.ZoneId, null)
                .Set(document => document.UpdatedAt, DateTime.UtcNow),
            cancellationToken: cancellationToken);

        return deleteZoneResult.DeletedCount > 0;
    }

    public async Task<ParkExplorerResult> GetExplorerAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkZoneDocument> zoneFilter = Builders<ParkZoneDocument>.Filter.Eq(document => document.ParkId, parkId);
        FilterDefinition<ParkItemDocument> itemFilter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId);

        if (!includeHidden)
        {
            zoneFilter &= Builders<ParkZoneDocument>.Filter.Eq(document => document.IsVisible, true);
            itemFilter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkZoneDocument> zoneDocuments = await this.zonesCollection.Find(zoneFilter)
            .SortBy(document => document.SortOrder)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        List<ParkItemDocument> itemDocuments = await this.itemsCollection.Find(itemFilter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        return new ParkExplorerResult
        {
            ParkId = parkId,
            Zones = zoneDocuments.Select(document => document.ToDomain()).ToList(),
            Items = itemDocuments.Select(document => document.ToDomain()).ToList(),
        };
    }
}

/// <summary>
/// Repository Mongo des éléments de parc.
/// </summary>
public sealed class ParkItemRepository : IParkItemRepository
{
    private readonly IMongoCollection<ParkItemDocument> collection;

    public ParkItemRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Empty;

        if (!string.IsNullOrWhiteSpace(parkId))
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId.Trim());
        }

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string escapedSearch = Regex.Escape(search.Trim());
            BsonRegularExpression regex = new BsonRegularExpression(escapedSearch, "i");

            List<FilterDefinition<ParkItemDocument>> searchFilters = new List<FilterDefinition<ParkItemDocument>>
            {
                Builders<ParkItemDocument>.Filter.Regex(document => document.Name, regex),
                Builders<ParkItemDocument>.Filter.Regex(document => document.Subtype, regex),
            };

            if (Enum.TryParse(search.Trim(), true, out ParkItemType parsedType))
            {
                searchFilters.Add(Builders<ParkItemDocument>.Filter.Eq(document => document.Type, parsedType));
            }

            if (Enum.TryParse(search.Trim(), true, out ParkItemCategory parsedCategory))
            {
                searchFilters.Add(Builders<ParkItemDocument>.Filter.Eq(document => document.Category, parsedCategory));
            }

            filter &= Builders<ParkItemDocument>.Filter.Or(searchFilters);
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.ParkId)
            .ThenBy(document => document.Name)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkItem>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<ParkItem?> GetByIdAsync(string parkItemId, CancellationToken cancellationToken)
    {
        ParkItemDocument? document = await this.collection.Find(document => document.Id == parkItemId)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<ParkItem> CreateAsync(ParkItem parkItem, CancellationToken cancellationToken)
    {
        ParkItemDocument document = parkItem.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<ParkItem?> UpdateAsync(string parkItemId, ParkItem parkItem, CancellationToken cancellationToken)
    {
        ParkItemDocument document = parkItem.ToDocument();
        document.Id = parkItemId;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            existing => existing.Id == parkItemId,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<bool> DeleteAsync(string parkItemId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == parkItemId, cancellationToken: cancellationToken);
        return result.DeletedCount > 0;
    }

    public async Task<IReadOnlyDictionary<string, int>> GetAttractionCountsByManufacturerIdsAsync(IEnumerable<string> manufacturerIds, CancellationToken cancellationToken)
    {
        List<string> normalizedManufacturerIds = manufacturerIds
            .Where(static manufacturerId => !string.IsNullOrWhiteSpace(manufacturerId))
            .Select(static manufacturerId => manufacturerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedManufacturerIds.Count == 0)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.Category, ParkItemCategory.Attraction) &
            Builders<ParkItemDocument>.Filter.In("attractionDetails.manufacturerId", normalizedManufacturerIds);

        List<BsonDocument> aggregationResults = await this.collection.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", "$attractionDetails.manufacturerId" },
                { "count", new BsonDocument("$sum", 1) },
            })
            .ToListAsync(cancellationToken);

        Dictionary<string, int> counts = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (BsonDocument aggregationResult in aggregationResults)
        {
            BsonValue manufacturerIdValue = aggregationResult.GetValue("_id", BsonNull.Value);
            if (!manufacturerIdValue.IsString)
            {
                continue;
            }

            string manufacturerId = manufacturerIdValue.AsString;
            if (string.IsNullOrWhiteSpace(manufacturerId))
            {
                continue;
            }

            counts[manufacturerId] = aggregationResult.GetValue("count", 0).ToInt32();
        }

        return counts;
    }
}
