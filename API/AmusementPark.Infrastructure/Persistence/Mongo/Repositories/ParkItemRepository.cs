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

    public async Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = this.BuildAdminListFilter(parkId, includeHidden, isVisible, adminReviewStatus, category, type);

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
            .Sort(this.BuildAdminListSort())
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkItem>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }


    public async Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);

        if (normalizedParkIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category) &
            Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds);
        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<BsonDocument> aggregationResults = await this.collection.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", new BsonDocument
                    {
                        { "parkId", "$parkId" },
                        { "category", "$category" },
                    }
                },
                { "count", new BsonDocument("$sum", 1) },
            })
            .ToListAsync(cancellationToken);

        Dictionary<string, Dictionary<ParkItemCategory, int>> mutableCounts = new Dictionary<string, Dictionary<ParkItemCategory, int>>(StringComparer.Ordinal);

        foreach (BsonDocument aggregationResult in aggregationResults)
        {
            BsonValue idValue = aggregationResult.GetValue("_id", new BsonDocument());
            if (!idValue.IsBsonDocument)
            {
                continue;
            }

            BsonDocument id = idValue.AsBsonDocument;
            BsonValue parkIdValue = id.GetValue("parkId", BsonValue.Create(string.Empty));
            BsonValue categoryValue = id.GetValue("category", BsonValue.Create(string.Empty));

            if (!parkIdValue.IsString || !categoryValue.IsString)
            {
                continue;
            }

            string parkId = parkIdValue.AsString;
            string categoryText = categoryValue.AsString;

            if (string.IsNullOrWhiteSpace(parkId) || !Enum.TryParse(categoryText, true, out ParkItemCategory category))
            {
                continue;
            }

            if (!mutableCounts.TryGetValue(parkId, out Dictionary<ParkItemCategory, int>? countsByCategory))
            {
                countsByCategory = new Dictionary<ParkItemCategory, int>();
                mutableCounts[parkId] = countsByCategory;
            }

            BsonValue countValue = aggregationResult.GetValue("count", BsonValue.Create(0));
            countsByCategory[category] = countValue.IsNumeric ? countValue.ToInt32() : 0;
        }

        return mutableCounts.ToDictionary(
            pair => pair.Key,
            pair => (IReadOnlyDictionary<ParkItemCategory, int>)pair.Value,
            StringComparer.Ordinal);
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

    public async Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkItemIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedParkItemIds = parkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkItemIds.Count == 0 || (!isVisible.HasValue && !adminReviewStatus.HasValue))
        {
            return 0;
        }

        UpdateDefinition<ParkItemDocument> update = Builders<ParkItemDocument>.Update.Set(document => document.UpdatedAt, DateTime.UtcNow);
        if (isVisible.HasValue)
        {
            update = update.Set(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            update = update.Set(document => document.AdminReviewStatus, adminReviewStatus.Value);
        }

        UpdateResult result = await this.collection.UpdateManyAsync(
            Builders<ParkItemDocument>.Filter.In(document => document.Id, normalizedParkItemIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }

    private FilterDefinition<ParkItemDocument> BuildAdminListFilter(string? parkId, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type)
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

        if (isVisible.HasValue)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            filter &= this.BuildAdminReviewStatusFilter(adminReviewStatus.Value);
        }

        if (category.HasValue)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category.Value);
        }

        if (type.HasValue)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.Type, type.Value);
        }

        return filter;
    }

    private FilterDefinition<ParkItemDocument> BuildAdminReviewStatusFilter(AdminReviewStatus adminReviewStatus)
    {
        if (adminReviewStatus == AdminReviewStatus.Ready)
        {
            return Builders<ParkItemDocument>.Filter.Or(
                Builders<ParkItemDocument>.Filter.Eq(document => document.AdminReviewStatus, AdminReviewStatus.Ready),
                Builders<ParkItemDocument>.Filter.Exists("adminReviewStatus", false));
        }

        return Builders<ParkItemDocument>.Filter.Eq(document => document.AdminReviewStatus, adminReviewStatus);
    }

    private SortDefinition<ParkItemDocument> BuildAdminListSort()
    {
        return Builders<ParkItemDocument>.Sort
            .Ascending(document => document.AdminReviewStatus)
            .Ascending(document => document.ParkId)
            .Ascending(document => document.Name)
            .Ascending(document => document.Id);
    }

    private static List<string> NormalizeParkIds(IEnumerable<string> parkIds)
    {
        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
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
