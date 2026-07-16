using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.StandaloneAttractions;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class StandaloneAttractionRepository : IStandaloneAttractionRepository
{
    private readonly IMongoCollection<StandaloneAttractionDocument> collection;

    public StandaloneAttractionRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<StandaloneAttractionDocument>(settings.StandaloneAttractionsCollectionName);
    }

    public async Task<StandaloneAttraction?> GetByIdAsync(string id, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<StandaloneAttractionDocument> filter = Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.Id, id);
        if (!includeHidden)
        {
            filter &= BuildPublicFilter();
        }

        StandaloneAttractionDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<StandaloneAttraction>> GetByIdsAsync(IReadOnlyCollection<string> ids, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(ids);
        if (normalizedIds.Count == 0)
        {
            return Array.Empty<StandaloneAttraction>();
        }

        List<StandaloneAttractionDocument> documents = await this.collection
            .Find(document => normalizedIds.Contains(document.Id))
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<StandaloneAttraction?> FindByLegacyAsync(string? legacyParkId, string? legacyParkItemId, CancellationToken cancellationToken)
    {
        FilterDefinition<StandaloneAttractionDocument> filter = Builders<StandaloneAttractionDocument>.Filter.Empty;
        bool hasFilter = false;

        if (!string.IsNullOrWhiteSpace(legacyParkId))
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.LegacyParkId, legacyParkId.Trim());
            hasFilter = true;
        }

        if (!string.IsNullOrWhiteSpace(legacyParkItemId))
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.LegacyParkItemId, legacyParkItemId.Trim());
            hasFilter = true;
        }

        if (!hasFilter)
        {
            return null;
        }

        StandaloneAttractionDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<StandaloneAttraction>> GetPageAsync(
        int page,
        int pageSize,
        string? search,
        bool includeHidden,
        bool? isVisible,
        AdminReviewStatus? adminReviewStatus,
        ParkItemType? type,
        string? countryCode,
        string? manufacturerId,
        CancellationToken cancellationToken,
        StandaloneAttractionAdminSortField sortField = StandaloneAttractionAdminSortField.Default,
        bool sortDescending = false)
    {
        FilterDefinition<StandaloneAttractionDocument> filter = this.BuildFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, manufacturerId);
        if (!string.IsNullOrWhiteSpace(search))
        {
            string escapedSearch = Regex.Escape(search.Trim());
            BsonRegularExpression regex = new BsonRegularExpression(escapedSearch, "i");
            filter &= Builders<StandaloneAttractionDocument>.Filter.Or(
                Builders<StandaloneAttractionDocument>.Filter.Regex(document => document.Name, regex),
                Builders<StandaloneAttractionDocument>.Filter.Regex(document => document.Subtype, regex),
                Builders<StandaloneAttractionDocument>.Filter.Regex(document => document.City, regex),
                Builders<StandaloneAttractionDocument>.Filter.Regex("descriptions.value", regex),
                Builders<StandaloneAttractionDocument>.Filter.Regex("attractionDetails.model", regex));
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<StandaloneAttractionDocument> documents = await this.collection.Find(filter)
            .Sort(this.BuildSort(sortField, sortDescending))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<StandaloneAttraction>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<StandaloneAttraction>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<StandaloneAttraction>();
        }

        List<StandaloneAttractionDocument> documents = await this.collection.Find(BuildPublicFilter())
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<string>> GetIdsByOperatorIdAsync(string operatorId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(operatorId))
        {
            return Array.Empty<string>();
        }

        return await this.collection
            .Find(Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.OperatorId, operatorId.Trim()))
            .Project(static document => document.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetIdsByManufacturerIdAsync(string manufacturerId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(manufacturerId))
        {
            return Array.Empty<string>();
        }

        return await this.collection
            .Find(Builders<StandaloneAttractionDocument>.Filter.Eq("attractionDetails.manufacturerId", manufacturerId.Trim()))
            .Project(static document => document.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task<StandaloneAttraction> CreateAsync(StandaloneAttraction attraction, CancellationToken cancellationToken)
    {
        StandaloneAttractionDocument document = attraction.ToDocument();
        document.Id = string.IsNullOrWhiteSpace(document.Id) ? Guid.NewGuid().ToString() : document.Id;
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;
        document.RefreshLocation();

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<StandaloneAttraction?> UpdateAsync(string id, StandaloneAttraction attraction, CancellationToken cancellationToken)
    {
        StandaloneAttractionDocument? existing = await this.collection.Find(document => document.Id == id)
            .Project(static document => new StandaloneAttractionDocument
            {
                Id = document.Id,
                CreatedAt = document.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            return null;
        }

        StandaloneAttractionDocument document = attraction.ToDocument();
        document.Id = id;
        document.CreatedAt = existing.CreatedAt;
        document.UpdatedAt = DateTime.UtcNow;
        document.RefreshLocation();

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            current => current.Id == id,
            document,
            cancellationToken: cancellationToken);

        if (result.MatchedCount == 0)
        {
            return null;
        }

        return document.ToDomain();
    }

    public async Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> ids, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(ids);
        if (normalizedIds.Count == 0 || (!isVisible.HasValue && !adminReviewStatus.HasValue))
        {
            return 0;
        }

        UpdateDefinition<StandaloneAttractionDocument> update = Builders<StandaloneAttractionDocument>.Update.Set(document => document.UpdatedAt, DateTime.UtcNow);
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
            Builders<StandaloneAttractionDocument>.Filter.In(document => document.Id, normalizedIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }

    private FilterDefinition<StandaloneAttractionDocument> BuildFilter(
        bool includeHidden,
        bool? isVisible,
        AdminReviewStatus? adminReviewStatus,
        ParkItemType? type,
        string? countryCode,
        string? manufacturerId)
    {
        FilterDefinition<StandaloneAttractionDocument> filter = includeHidden
            ? Builders<StandaloneAttractionDocument>.Filter.Empty
            : BuildPublicFilter();

        if (isVisible.HasValue)
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.BuildAdminReviewStatusFilter("adminReviewStatus", adminReviewStatus.Value);
        }

        if (type.HasValue)
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.Type, type.Value);
        }

        if (!string.IsNullOrWhiteSpace(countryCode))
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.CountryCode, countryCode.Trim().ToUpperInvariant());
        }

        if (!string.IsNullOrWhiteSpace(manufacturerId))
        {
            filter &= Builders<StandaloneAttractionDocument>.Filter.Eq("attractionDetails.manufacturerId", manufacturerId.Trim());
        }

        return filter;
    }

    private SortDefinition<StandaloneAttractionDocument> BuildSort(StandaloneAttractionAdminSortField sortField, bool sortDescending)
    {
        SortDefinitionBuilder<StandaloneAttractionDocument> sortBuilder = Builders<StandaloneAttractionDocument>.Sort;
        SortDefinition<StandaloneAttractionDocument> primarySort = sortField switch
        {
            StandaloneAttractionAdminSortField.Name => sortDescending ? sortBuilder.Descending(document => document.Name) : sortBuilder.Ascending(document => document.Name),
            StandaloneAttractionAdminSortField.Type => sortDescending ? sortBuilder.Descending(document => document.Type) : sortBuilder.Ascending(document => document.Type),
            StandaloneAttractionAdminSortField.CountryCode => sortDescending ? sortBuilder.Descending(document => document.CountryCode) : sortBuilder.Ascending(document => document.CountryCode),
            StandaloneAttractionAdminSortField.IsVisible => sortDescending ? sortBuilder.Descending(document => document.IsVisible) : sortBuilder.Ascending(document => document.IsVisible),
            StandaloneAttractionAdminSortField.AdminReviewStatus => sortDescending ? sortBuilder.Descending(document => document.AdminReviewPriority) : sortBuilder.Ascending(document => document.AdminReviewPriority),
            _ => sortBuilder.Ascending(document => document.AdminReviewPriority),
        };

        return sortBuilder.Combine(
            primarySort,
            sortBuilder.Ascending(document => document.Name),
            sortBuilder.Ascending(document => document.Id));
    }

    private static FilterDefinition<StandaloneAttractionDocument> BuildPublicFilter()
    {
        return Builders<StandaloneAttractionDocument>.Filter.Eq(document => document.IsVisible, true)
            & Builders<StandaloneAttractionDocument>.Filter.Ne(document => document.AdminReviewStatus, AdminReviewStatus.NotRelevant)
            & Builders<StandaloneAttractionDocument>.Filter.Not(
                Builders<StandaloneAttractionDocument>.Filter.Regex(
                    "attractionDetails.status",
                    new BsonRegularExpression("^(closed\\s*definitively|closed-definitively|closed_definitively|closeddefinitively|permanently\\s*closed|permanently-closed|permanently_closed|permanentlyclosed|definitively\\s*closed|definitively-closed|definitively_closed|definitivelyclosed|ferme\\s*definitivement|fermÃ©\\s*dÃ©finitivement|fermedefinitivement)$", "i")));
    }

    private static List<string> NormalizeIds(IEnumerable<string> ids)
    {
        return ids
            .Where(static id => !string.IsNullOrWhiteSpace(id))
            .Select(static id => id.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }
}
