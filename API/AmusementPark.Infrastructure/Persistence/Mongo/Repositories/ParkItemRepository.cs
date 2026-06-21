using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.ParkItems;
using AmusementPark.Application.Features.ParkItems.Contracts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.ParkZones.Results;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
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
        return await this.GetByParkIdAsync(parkId, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<ParkItem>> GetPublicPageByParkIdAsync(
        int page,
        int pageSize,
        string parkId,
        string? search,
        bool includeHidden,
        ClosedEntityFilter closedFilter,
        ParkItemCategory? category,
        ParkItemType? type,
        string? zoneId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = BuildPublicParkItemsFilter(parkId, search, includeHidden, closedFilter, category, type, zoneId);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .Project(static document => new ParkItemDocument
            {
                Id = document.Id,
                ParkId = document.ParkId,
                ZoneId = document.ZoneId,
                Name = document.Name,
                Category = document.Category,
                Type = document.Type,
                Subtype = document.Subtype,
                Latitude = document.Latitude,
                Longitude = document.Longitude,
                Descriptions = document.Descriptions,
                AttractionDetails = document.AttractionDetails,
                IsVisible = document.IsVisible,
                AdminReviewStatus = document.AdminReviewStatus,
            })
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ParkItem>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetByParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<ParkItem>();
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.ParkId)
            .ThenBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        return await this.GetNavigationItemsByParkIdAsync(parkId, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<IReadOnlyList<ParkItemSiblingNavigationItem>> GetNavigationItemsByParkIdAsync(string parkId, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        List<ParkItemSiblingNavigationItem> items = await this.collection.Find(filter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Project(document => new ParkItemSiblingNavigationItem
            {
                Id = document.Id,
                Name = document.Name,
            })
            .ToListAsync(cancellationToken);

        return items;
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, CancellationToken cancellationToken)
    {
        return await this.GetRelatedItemsAsync(currentItem, limit, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetRelatedItemsAsync(ParkItem currentItem, int limit, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        if (limit <= 0 || string.IsNullOrWhiteSpace(currentItem.Id) || string.IsNullOrWhiteSpace(currentItem.ParkId))
        {
            return Array.Empty<ParkItem>();
        }

        FilterDefinitionBuilder<ParkItemDocument> filterBuilder = Builders<ParkItemDocument>.Filter;
        FilterDefinition<ParkItemDocument> filter =
            filterBuilder.Eq(document => document.ParkId, currentItem.ParkId) &
            filterBuilder.Ne(document => document.Id, currentItem.Id);

        if (!includeHidden)
        {
            filter &= filterBuilder.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        List<FilterDefinition<ParkItemDocument>> affinityFilters = new List<FilterDefinition<ParkItemDocument>>
        {
            filterBuilder.Eq(document => document.Category, currentItem.Category),
            filterBuilder.Eq(document => document.Type, currentItem.Type),
        };

        if (!string.IsNullOrWhiteSpace(currentItem.ZoneId))
        {
            affinityFilters.Add(filterBuilder.Eq(document => document.ZoneId, currentItem.ZoneId));
        }

        filter &= filterBuilder.Or(affinityFilters);

        int candidateLimit = Math.Max(limit * 8, 24);
        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.Category)
            .ThenBy(document => document.Type)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Limit(candidateLimit)
            .ToListAsync(cancellationToken);

        return documents
            .Select(document => document.ToDomain())
            .OrderByDescending(item => CalculateRelatedScore(item, currentItem))
            .ThenBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(item => item.Id, StringComparer.Ordinal)
            .Take(limit)
            .ToList();
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<ParkItem>();
        }

        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true) &
            Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant);

        List<ParkItemDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.ParkId)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<ParkItem>> GetPageAsync(int page, int pageSize, string? parkId, string? search, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type, string? zoneId, string? manufacturerId, ParkItemContentBacklogFilter? contentBacklogFilter, CancellationToken cancellationToken, ParkItemAdminSortField sortField = ParkItemAdminSortField.Default, bool sortDescending = false)
    {
        FilterDefinition<ParkItemDocument> filter = this.BuildAdminListFilter(parkId, includeHidden, isVisible, adminReviewStatus, category, type, zoneId, manufacturerId, contentBacklogFilter);

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
            .Sort(this.BuildAdminListSort(sortField, sortDescending))
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
        return await this.CountByCategoryAsync(category, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<long> CountByCategoryAsync(ParkItemCategory category, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category) &
            Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }


    public async Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        return await this.CountByCategoryForParkIdsAsync(category, parkIds, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<long> CountByCategoryForParkIdsAsync(ParkItemCategory category, IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);

        if (normalizedParkIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category) &
            Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds) &
            Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, CancellationToken cancellationToken)
    {
        return await this.GetCountsByCategoryForParkIdsAsync(parkIds, includeHidden, ClosedEntityFilter.All, cancellationToken);
    }

    public async Task<IReadOnlyDictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>> GetCountsByCategoryForParkIdsAsync(IReadOnlyCollection<string> parkIds, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return new Dictionary<string, IReadOnlyDictionary<ParkItemCategory, int>>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds) &
            Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant);
        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

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

    public async Task<IReadOnlyDictionary<string, ParkItemVisibilityCounts>> GetVisibilityCountsByParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return new Dictionary<string, ParkItemVisibilityCounts>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(document => document.ParkId, normalizedParkIds);

        List<BsonDocument> aggregationResults = await this.collection.Aggregate()
            .Match(filter)
            .Group(new BsonDocument
            {
                { "_id", "$parkId" },
                { "totalCount", new BsonDocument("$sum", 1) },
                { "visibleCount", new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray { "$isVisible", 1, 0 })) },
            })
            .ToListAsync(cancellationToken);

        Dictionary<string, ParkItemVisibilityCounts> counts = new Dictionary<string, ParkItemVisibilityCounts>(StringComparer.Ordinal);

        foreach (BsonDocument aggregationResult in aggregationResults)
        {
            BsonValue parkIdValue = aggregationResult.GetValue("_id", BsonNull.Value);
            if (!parkIdValue.IsString || string.IsNullOrWhiteSpace(parkIdValue.AsString))
            {
                continue;
            }

            counts[parkIdValue.AsString] = new ParkItemVisibilityCounts
            {
                TotalCount = aggregationResult.GetValue("totalCount", 0).ToInt32(),
                VisibleCount = aggregationResult.GetValue("visibleCount", 0).ToInt32(),
            };
        }

        return counts;
    }

    public Task<ParkItem?> GetByIdAsync(string parkItemId, CancellationToken cancellationToken)
    {
        return this.GetByIdAsync(parkItemId, true, cancellationToken);
    }

    public async Task<ParkItem?> GetByIdAsync(string parkItemId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(document => document.Id, parkItemId);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        ParkItemDocument? document = await this.collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);

        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<ParkItem>> GetByIdsAsync(IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkItemIds = parkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkItemIds.Count == 0)
        {
            return Array.Empty<ParkItem>();
        }

        List<ParkItemDocument> documents = await this.collection
            .Find(Builders<ParkItemDocument>.Filter.In(document => document.Id, normalizedParkItemIds))
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<string>> GetParkIdsByManufacturerIdAsync(string manufacturerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturerId))
        {
            return Array.Empty<string>();
        }

        string normalizedManufacturerId = manufacturerId.Trim();
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.Eq(
            document => document.AttractionDetails!.ManufacturerId,
            normalizedManufacturerId);

        List<string> parkIds = await this.collection.Find(filter)
            .Project(document => document.ParkId)
            .ToListAsync(cancellationToken);

        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
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
            AdminReviewStatus normalizedStatus = adminReviewStatus.Value.NormalizeForAdministration();
            update = update
                .Set(document => document.AdminReviewStatus, normalizedStatus)
                .Set(document => document.AdminReviewPriority, normalizedStatus.ToAdminReviewPriority());
        }

        UpdateResult result = await this.collection.UpdateManyAsync(
            Builders<ParkItemDocument>.Filter.In(document => document.Id, normalizedParkItemIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }

    public async Task<int> UpdateBulkFieldsAsync(IReadOnlyCollection<string> parkItemIds, bool updateZone, string? zoneId, ParkItemCategory? category, ParkItemType? type, bool updateManufacturer, string? manufacturerId, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedParkItemIds = parkItemIds
            .Where(static parkItemId => !string.IsNullOrWhiteSpace(parkItemId))
            .Select(static parkItemId => parkItemId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkItemIds.Count == 0 || (!updateZone && !category.HasValue && !type.HasValue && !updateManufacturer && !isVisible.HasValue && !adminReviewStatus.HasValue))
        {
            return 0;
        }

        UpdateDefinition<ParkItemDocument> update = Builders<ParkItemDocument>.Update.Set(document => document.UpdatedAt, DateTime.UtcNow);
        if (updateZone)
        {
            update = string.IsNullOrWhiteSpace(zoneId)
                ? update.Unset(document => document.ZoneId)
                : update.Set(document => document.ZoneId, zoneId.Trim());
        }

        if (category.HasValue)
        {
            update = update.Set(document => document.Category, category.Value);
        }

        if (type.HasValue)
        {
            update = update.Set(document => document.Type, type.Value);
        }

        if (updateManufacturer)
        {
            update = string.IsNullOrWhiteSpace(manufacturerId)
                ? update.Unset("attractionDetails.manufacturerId")
                : update.Set("attractionDetails.manufacturerId", manufacturerId.Trim());
        }

        if (isVisible.HasValue)
        {
            update = update.Set(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            AdminReviewStatus normalizedStatus = adminReviewStatus.Value.NormalizeForAdministration();
            update = update
                .Set(document => document.AdminReviewStatus, normalizedStatus)
                .Set(document => document.AdminReviewPriority, normalizedStatus.ToAdminReviewPriority());
        }

        UpdateResult result = await this.collection.UpdateManyAsync(
            Builders<ParkItemDocument>.Filter.In(document => document.Id, normalizedParkItemIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }

    private FilterDefinition<ParkItemDocument> BuildAdminListFilter(string? parkId, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkItemCategory? category, ParkItemType? type, string? zoneId, string? manufacturerId, ParkItemContentBacklogFilter? contentBacklogFilter)
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

        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, zoneId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(manufacturerId))
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq("attractionDetails.manufacturerId", manufacturerId.Trim());
        }

        if (contentBacklogFilter.HasValue)
        {
            filter &= BuildContentBacklogFilter(contentBacklogFilter.Value);
        }

        return filter;
    }

    private static FilterDefinition<ParkItemDocument> BuildContentBacklogFilter(ParkItemContentBacklogFilter contentBacklogFilter)
    {
        switch (contentBacklogFilter)
        {
            case ParkItemContentBacklogFilter.MissingDescriptionFr:
                return BuildMissingDescriptionFilter("fr");
            case ParkItemContentBacklogFilter.MissingDescriptionEn:
                return BuildMissingDescriptionFilter("en");
            case ParkItemContentBacklogFilter.MissingAnyDescription:
                return Builders<ParkItemDocument>.Filter.Not(BuildHasAnyDescriptionFilter());
            case ParkItemContentBacklogFilter.MissingZone:
                return Builders<ParkItemDocument>.Filter.Or(
                    Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, null),
                    Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, string.Empty));
            case ParkItemContentBacklogFilter.MissingPreciseType:
                return Builders<ParkItemDocument>.Filter.In(document => document.Type, new[] { ParkItemType.Attraction, ParkItemType.Other });
            case ParkItemContentBacklogFilter.VisibleIncomplete:
                return Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true)
                    & Builders<ParkItemDocument>.Filter.Or(
                        Builders<ParkItemDocument>.Filter.Eq(document => document.Category, ParkItemCategory.Other),
                        Builders<ParkItemDocument>.Filter.In(document => document.Type, new[] { ParkItemType.Attraction, ParkItemType.Other }),
                        Builders<ParkItemDocument>.Filter.Not(BuildHasAnyDescriptionFilter()),
                        Builders<ParkItemDocument>.Filter.Or(
                            Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, null),
                            Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, string.Empty)));
            default:
                return Builders<ParkItemDocument>.Filter.Empty;
        }
    }

    private static FilterDefinition<ParkItemDocument> BuildPublicParkItemsFilter(
        string parkId,
        string? search,
        bool includeHidden,
        ClosedEntityFilter closedFilter,
        ParkItemCategory? category,
        ParkItemType? type,
        string? zoneId)
    {
        FilterDefinition<ParkItemDocument> filter =
            Builders<ParkItemDocument>.Filter.Eq(document => document.ParkId, parkId)
            & Builders<ParkItemDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant);

        if (!includeHidden)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        filter &= BuildClosedFilter(closedFilter);

        if (category.HasValue)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.Category, category.Value);
        }

        if (type.HasValue)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.Type, type.Value);
        }

        if (!string.IsNullOrWhiteSpace(zoneId))
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.ZoneId, zoneId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            string escapedSearch = Regex.Escape(search.Trim());
            BsonRegularExpression regex = new BsonRegularExpression(escapedSearch, "i");
            filter &= Builders<ParkItemDocument>.Filter.Or(
                Builders<ParkItemDocument>.Filter.Regex(document => document.Name, regex),
                Builders<ParkItemDocument>.Filter.Regex(document => document.Subtype, regex),
                Builders<ParkItemDocument>.Filter.Regex("descriptions.value", regex),
                Builders<ParkItemDocument>.Filter.Regex("attractionDetails.model", regex),
                Builders<ParkItemDocument>.Filter.Regex("attractionDetails.status", regex));
        }

        return filter;
    }

    private static FilterDefinition<ParkItemDocument> BuildMissingDescriptionFilter(string languageCode)
    {
        return Builders<ParkItemDocument>.Filter.Not(BuildHasDescriptionFilter(languageCode));
    }

    private static FilterDefinition<ParkItemDocument> BuildHasDescriptionFilter(string languageCode)
    {
        FilterDefinition<LocalizedTextDocument> localizedFilter =
            Builders<LocalizedTextDocument>.Filter.Eq(document => document.LanguageCode, languageCode)
            & Builders<LocalizedTextDocument>.Filter.Regex(document => document.Value, new BsonRegularExpression("\\S"));

        return Builders<ParkItemDocument>.Filter.ElemMatch(document => document.Descriptions, localizedFilter);
    }

    private static FilterDefinition<ParkItemDocument> BuildHasAnyDescriptionFilter()
    {
        FilterDefinition<LocalizedTextDocument> localizedFilter =
            Builders<LocalizedTextDocument>.Filter.Regex(document => document.Value, new BsonRegularExpression("\\S"));

        return Builders<ParkItemDocument>.Filter.ElemMatch(document => document.Descriptions, localizedFilter);
    }

    private FilterDefinition<ParkItemDocument> BuildAdminReviewStatusFilter(AdminReviewStatus adminReviewStatus)
    {
        return Builders<ParkItemDocument>.Filter.BuildAdminReviewStatusFilter("adminReviewStatus", adminReviewStatus);
    }

    private SortDefinition<ParkItemDocument> BuildAdminListSort(ParkItemAdminSortField sortField, bool sortDescending)
    {
        SortDefinitionBuilder<ParkItemDocument> sortBuilder = Builders<ParkItemDocument>.Sort;

        if (sortField == ParkItemAdminSortField.Default)
        {
            return sortBuilder
                .Ascending(document => document.AdminReviewPriority)
                .Ascending(document => document.ParkId)
                .Ascending(document => document.Name)
                .Ascending(document => document.Id);
        }

        SortDefinition<ParkItemDocument> primarySort = this.BuildPrimaryAdminListSort(sortField, sortDescending, sortBuilder);
        return sortBuilder.Combine(
            primarySort,
            sortBuilder.Ascending(document => document.Name),
            sortBuilder.Ascending(document => document.Id));
    }

    private SortDefinition<ParkItemDocument> BuildPrimaryAdminListSort(ParkItemAdminSortField sortField, bool sortDescending, SortDefinitionBuilder<ParkItemDocument> sortBuilder)
    {
        switch (sortField)
        {
            case ParkItemAdminSortField.Name:
                return sortDescending ? sortBuilder.Descending(document => document.Name) : sortBuilder.Ascending(document => document.Name);
            case ParkItemAdminSortField.Category:
                return sortDescending ? sortBuilder.Descending(document => document.Category) : sortBuilder.Ascending(document => document.Category);
            case ParkItemAdminSortField.Type:
                return sortDescending ? sortBuilder.Descending(document => document.Type) : sortBuilder.Ascending(document => document.Type);
            case ParkItemAdminSortField.IsVisible:
                return sortDescending ? sortBuilder.Descending(document => document.IsVisible) : sortBuilder.Ascending(document => document.IsVisible);
            case ParkItemAdminSortField.AdminReviewStatus:
                return sortDescending ? sortBuilder.Descending(document => document.AdminReviewPriority) : sortBuilder.Ascending(document => document.AdminReviewPriority);
            case ParkItemAdminSortField.ParkId:
                return sortDescending ? sortBuilder.Descending(document => document.ParkId) : sortBuilder.Ascending(document => document.ParkId);
            case ParkItemAdminSortField.ZoneId:
                return sortDescending ? sortBuilder.Descending(document => document.ZoneId) : sortBuilder.Ascending(document => document.ZoneId);
            default:
                return sortBuilder.Ascending(document => document.AdminReviewPriority);
        }
    }

    private static int CalculateRelatedScore(ParkItem candidate, ParkItem currentItem)
    {
        int score = 0;

        if (candidate.Category == currentItem.Category)
        {
            score += 4;
        }

        if (candidate.Type == currentItem.Type)
        {
            score += 3;
        }

        if (!string.IsNullOrWhiteSpace(currentItem.ZoneId) && string.Equals(candidate.ZoneId, currentItem.ZoneId, StringComparison.Ordinal))
        {
            score += 2;
        }

        return score;
    }

    private static List<string> NormalizeParkIds(IEnumerable<string> parkIds)
    {
        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static FilterDefinition<ParkItemDocument> BuildClosedFilter(ClosedEntityFilter closedFilter)
    {
        FilterDefinition<ParkItemDocument> closedFilterDefinition = Builders<ParkItemDocument>.Filter.Regex(
            "attractionDetails.status",
            new BsonRegularExpression("^(closed\\s*definitively|closed-definitively|closed_definitively|closeddefinitively|permanently\\s*closed|permanently-closed|permanently_closed|permanentlyclosed|definitively\\s*closed|definitively-closed|definitively_closed|definitivelyclosed|ferme\\s*definitivement|fermé\\s*définitivement|fermedefinitivement)$", "i"));

        return closedFilter switch
        {
            ClosedEntityFilter.All => Builders<ParkItemDocument>.Filter.Empty,
            ClosedEntityFilter.ClosedOnly => closedFilterDefinition,
            _ => Builders<ParkItemDocument>.Filter.Not(closedFilterDefinition),
        };
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
