using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Parks.Contracts;
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
    private const int RandomVisibleFallbackHardLimit = 100;

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

    public async Task<IReadOnlyCollection<Park>> GetByIdsAsync(IEnumerable<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<Park>();
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(document => document.Id, normalizedParkIds);
        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Sort(this.BuildAdminListSort())
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<long> CountAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden);
        return await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
    }

    public async Task<IReadOnlyCollection<string>> GetVisibleParkIdsAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);

        List<string> parkIds = await this.collection.Find(filter)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        return NormalizeParkIds(parkIds);
    }

    public Task<IReadOnlyCollection<string>> GetParkIdsByOperatorIdAsync(string operatorId, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.OperatorId, operatorId);
        return this.GetParkIdsByFilterAsync(filter, cancellationToken);
    }

    public Task<IReadOnlyCollection<string>> GetParkIdsByFounderIdAsync(string founderId, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.FounderId, founderId);
        return this.GetParkIdsByFilterAsync(filter, cancellationToken);
    }

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, CancellationToken cancellationToken)
    {
        ParkSearchCriteria criteria = new ParkSearchCriteria(searchTerm, Array.Empty<string>(), Array.Empty<string>());
        return this.GetVisibleMapPointsAsync(criteria, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true)
            & Builders<ParkDocument>.Filter.Ne(document => document.Latitude, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.Longitude, null)
            & this.BuildCriteriaFilter(criteria);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Project(BuildMapPointProjection())
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetVisibleWithValidCoordinatesAsync(CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true)
            & BuildValidCoordinatesFilter();

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Project(BuildMapPointProjection())
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, CancellationToken cancellationToken)
    {
        return this.GetRandomVisibleAsync(limit, Array.Empty<string>(), cancellationToken);
    }

    public async Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        int safeLimit = Math.Min(limit, RandomVisibleFallbackHardLimit);
        FilterDefinition<ParkDocument> filter = this.BuildVisibleSelectionFilter(excludedParkIds);
        List<ParkDocument> documents = await this.LoadRandomVisibleWindowAsync(filter, safeLimit, cancellationToken);

        if (documents.Count == 0)
        {
            documents = await this.LoadRandomVisibleFallbackAsync(filter, safeLimit, cancellationToken);
        }

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        FilterDefinition<ParkDocument> filter = this.BuildVisibleSelectionFilter(excludedParkIds)
            & Builders<ParkDocument>.Filter.Eq(document => document.IsFeaturedOnHome, true);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.FeaturedHomeOrder)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<int> CountDistinctCountryCodesAsync(bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, string.Empty);

        IAsyncCursor<string?> cursor = await this.collection.DistinctAsync(
            document => document.CountryCode,
            filter,
            cancellationToken: cancellationToken);

        List<string?> countryCodes = await cursor.ToListAsync(cancellationToken);
        return countryCodes
            .Where(static countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(static countryCode => countryCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    public async Task<int> CountDistinctCountryCodesForParkIdsAsync(IReadOnlyCollection<string> parkIds, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);

        if (normalizedParkIds.Count == 0)
        {
            return 0;
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(document => document.Id, normalizedParkIds)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.CountryCode, string.Empty);

        IAsyncCursor<string?> cursor = await this.collection.DistinctAsync(
            document => document.CountryCode,
            filter,
            cancellationToken: cancellationToken);

        List<string?> countryCodes = await cursor.ToListAsync(cancellationToken);
        return countryCodes
            .Where(static countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(static countryCode => countryCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .Count();
    }

    public Task<PagedResult<Park>> SearchByNameAsync(string name, int page, int pageSize, bool includeHidden, CancellationToken cancellationToken)
    {
        ParkSearchCriteria criteria = new ParkSearchCriteria(name, Array.Empty<string>(), Array.Empty<string>());
        return this.SearchAsync(criteria, page, pageSize, includeHidden, null, null, null, null, null, cancellationToken);
    }

    public async Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates)
            & this.BuildCriteriaFilter(criteria);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Sort(this.BuildAdminListSort())
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
        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = BuildGeoJsonPoint(latitude, longitude);
        FilterDefinition<ParkDocument> filter = this.BuildNearLocationFilter(center, radiusInKilometers, includeHidden);

        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = BuildGeoJsonPoint(latitude, longitude);
        FilterDefinition<ParkDocument> filter = this.BuildNearLocationFilter(center, maxDistanceInKilometers, includeHidden);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<Park> CreateAsync(Park park, CancellationToken cancellationToken)
    {
        ParkDocument document = park.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;
        document.RandomSortKey = CreateRandomSortKey();

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken)
    {
        ParkDocument? existing = await this.collection.Find(document => document.Id == parkId)
            .Project(static document => new ParkDocument
            {
                Id = document.Id,
                CreatedAt = document.CreatedAt,
                RandomSortKey = document.RandomSortKey,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            return null;
        }

        ParkDocument document = park.ToDocument();
        document.Id = parkId;
        document.CreatedAt = existing.CreatedAt;
        document.UpdatedAt = DateTime.UtcNow;
        document.RandomSortKey = existing.RandomSortKey ?? CreateRandomSortKey();

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            current => current.Id == parkId,
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

    public async Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0 || (!isVisible.HasValue && !adminReviewStatus.HasValue))
        {
            return 0;
        }

        UpdateDefinition<ParkDocument> update = Builders<ParkDocument>.Update.Set(document => document.UpdatedAt, DateTime.UtcNow);
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
            Builders<ParkDocument>.Filter.In(document => document.Id, normalizedParkIds),
            update,
            cancellationToken: cancellationToken);

        return checked((int)result.ModifiedCount);
    }

    public async Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates);

        List<string> parkIds = await this.collection.Find(filter)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        return NormalizeParkIds(parkIds);
    }

    private async Task<IReadOnlyCollection<string>> GetParkIdsByFilterAsync(FilterDefinition<ParkDocument> filter, CancellationToken cancellationToken)
    {
        List<string> parkIds = await this.collection.Find(filter)
            .Project(document => document.Id)
            .ToListAsync(cancellationToken);

        return NormalizeParkIds(parkIds);
    }

    private static GeoJsonPoint<GeoJson2DGeographicCoordinates> BuildGeoJsonPoint(double latitude, double longitude)
    {
        return new GeoJsonPoint<GeoJson2DGeographicCoordinates>(
            new GeoJson2DGeographicCoordinates(longitude, latitude));
    }

    private static ProjectionDefinition<ParkDocument, ParkDocument> BuildMapPointProjection()
    {
        return Builders<ParkDocument>.Projection.Expression(static document => new ParkDocument
        {
            Id = document.Id,
            Name = document.Name,
            CountryCode = document.CountryCode,
            Street = document.Street,
            City = document.City,
            PostalCode = document.PostalCode,
            Latitude = document.Latitude,
            Longitude = document.Longitude,
            CurrentLogoImageId = document.CurrentLogoImageId,
            IsVisible = document.IsVisible,
            CreatedAt = document.CreatedAt,
            UpdatedAt = document.UpdatedAt,
        });
    }

    private async Task<List<ParkDocument>> LoadRandomVisibleWindowAsync(
        FilterDefinition<ParkDocument> baseFilter,
        int limit,
        CancellationToken cancellationToken)
    {
        double seed = CreateRandomSortKey();
        FilterDefinition<ParkDocument> randomKeyExistsFilter = Builders<ParkDocument>.Filter.Ne(document => document.RandomSortKey, null);
        FilterDefinition<ParkDocument> primaryFilter = baseFilter
            & randomKeyExistsFilter
            & Builders<ParkDocument>.Filter.Gte(document => document.RandomSortKey, seed);

        List<ParkDocument> documents = await this.collection.Find(primaryFilter)
            .SortBy(document => document.RandomSortKey)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        if (documents.Count >= limit)
        {
            return documents;
        }

        HashSet<string> existingIds = documents.Select(document => document.Id).ToHashSet(StringComparer.Ordinal);
        FilterDefinition<ParkDocument> secondaryFilter = baseFilter
            & randomKeyExistsFilter
            & Builders<ParkDocument>.Filter.Lt(document => document.RandomSortKey, seed);

        if (existingIds.Count > 0)
        {
            secondaryFilter &= Builders<ParkDocument>.Filter.Nin(document => document.Id, existingIds);
        }

        List<ParkDocument> secondaryDocuments = await this.collection.Find(secondaryFilter)
            .SortBy(document => document.RandomSortKey)
            .ThenBy(document => document.Id)
            .Limit(limit - documents.Count)
            .ToListAsync(cancellationToken);

        documents.AddRange(secondaryDocuments);
        return documents;
    }

    private async Task<List<ParkDocument>> LoadRandomVisibleFallbackAsync(
        FilterDefinition<ParkDocument> filter,
        int limit,
        CancellationToken cancellationToken)
    {
        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        if (totalItems <= 0)
        {
            return new List<ParkDocument>();
        }

        int effectiveLimit = checked((int)Math.Min(limit, totalItems));
        int maxSkip = checked((int)Math.Max(0L, totalItems - effectiveLimit));
        int skip = maxSkip == 0 ? 0 : Random.Shared.Next(0, maxSkip + 1);

        return await this.collection.Find(filter)
            .SortBy(document => document.Id)
            .Skip(skip)
            .Limit(effectiveLimit)
            .ToListAsync(cancellationToken);
    }

    private FilterDefinition<ParkDocument> BuildNearLocationFilter(GeoJsonPoint<GeoJson2DGeographicCoordinates> center, double? radiusInKilometers, bool includeHidden)
    {
        double? maxDistanceInMeters = radiusInKilometers.HasValue
            ? Math.Max(0d, radiusInKilometers.Value) * 1000d
            : null;

        FilterDefinition<ParkDocument> filter = maxDistanceInMeters.HasValue
            ? Builders<ParkDocument>.Filter.NearSphere(document => document.Location, center, maxDistance: maxDistanceInMeters.Value)
            : Builders<ParkDocument>.Filter.NearSphere(document => document.Location, center);

        filter &= Builders<ParkDocument>.Filter.Ne(document => document.Latitude, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.Longitude, null);

        if (!includeHidden)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return filter;
    }

    private FilterDefinition<ParkDocument> BuildCriteriaFilter(ParkSearchCriteria? criteria)
    {
        if (criteria is null || !criteria.HasAnyFilter)
        {
            return Builders<ParkDocument>.Filter.Empty;
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Empty;
        List<string> regionCountryCodes = NormalizeCountryCodes(criteria.RegionCountryCodes);
        if (regionCountryCodes.Count > 0)
        {
            filter &= Builders<ParkDocument>.Filter.In(document => document.CountryCode, regionCountryCodes);
        }

        FilterDefinition<ParkDocument>? searchFilter = this.BuildSearchTermFilter(criteria);
        if (searchFilter is not null)
        {
            filter &= searchFilter;
        }

        return filter;
    }

    private FilterDefinition<ParkDocument>? BuildSearchTermFilter(ParkSearchCriteria criteria)
    {
        string normalizedTerm = (criteria.SearchTerm ?? string.Empty).Trim();
        List<string> matchingCountryCodes = NormalizeCountryCodes(criteria.MatchingCountryCodes);

        if (normalizedTerm.Length == 0 && matchingCountryCodes.Count == 0)
        {
            return null;
        }

        List<FilterDefinition<ParkDocument>> filters = new List<FilterDefinition<ParkDocument>>();

        if (normalizedTerm.Length > 0)
        {
            string escapedTerm = Regex.Escape(normalizedTerm);
            BsonRegularExpression expression = new BsonRegularExpression(escapedTerm, "i");
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.Name, expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.City, expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.CountryCode, expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.Street, expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.PostalCode, expression));
        }

        if (matchingCountryCodes.Count > 0)
        {
            filters.Add(Builders<ParkDocument>.Filter.In(document => document.CountryCode, matchingCountryCodes));
        }

        return filters.Count == 0
            ? null
            : Builders<ParkDocument>.Filter.Or(filters);
    }

    private static List<string> NormalizeCountryCodes(IEnumerable<string> countryCodes)
    {
        return countryCodes
            .Where(static countryCode => !string.IsNullOrWhiteSpace(countryCode))
            .Select(static countryCode => countryCode.Trim().ToUpperInvariant())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private FilterDefinition<ParkDocument> BuildAdminListFilter(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden);

        if (isVisible.HasValue)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            filter &= this.BuildAdminReviewStatusFilter(adminReviewStatus.Value);
        }

        if (type.HasValue)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.Type, type.Value);
        }

        string normalizedCountryCode = (countryCode ?? string.Empty).Trim().ToUpperInvariant();
        if (normalizedCountryCode.Length > 0)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.CountryCode, normalizedCountryCode);
        }

        if (hasValidCoordinates.HasValue)
        {
            filter &= hasValidCoordinates.Value
                ? BuildValidCoordinatesFilter()
                : BuildInvalidCoordinatesFilter();
        }

        return filter;
    }

    private static FilterDefinition<ParkDocument> BuildValidCoordinatesFilter()
    {
        return Builders<ParkDocument>.Filter.Ne(document => document.Latitude, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.Longitude, null)
            & Builders<ParkDocument>.Filter.Or(
                Builders<ParkDocument>.Filter.Ne(document => document.Latitude, 0d),
                Builders<ParkDocument>.Filter.Ne(document => document.Longitude, 0d));
    }

    private static FilterDefinition<ParkDocument> BuildInvalidCoordinatesFilter()
    {
        return Builders<ParkDocument>.Filter.Or(
            Builders<ParkDocument>.Filter.Eq(document => document.Latitude, null),
            Builders<ParkDocument>.Filter.Eq(document => document.Longitude, null),
            Builders<ParkDocument>.Filter.And(
                Builders<ParkDocument>.Filter.Eq(document => document.Latitude, 0d),
                Builders<ParkDocument>.Filter.Eq(document => document.Longitude, 0d)));
    }

    private FilterDefinition<ParkDocument> BuildAdminReviewStatusFilter(AdminReviewStatus adminReviewStatus)
    {
        return Builders<ParkDocument>.Filter.BuildAdminReviewStatusFilter("adminReviewStatus", adminReviewStatus);
    }

    private SortDefinition<ParkDocument> BuildAdminListSort()
    {
        return Builders<ParkDocument>.Sort
            .Ascending(document => document.AdminReviewPriority)
            .Ascending(document => document.Name)
            .Ascending(document => document.Id);
    }

    private FilterDefinition<ParkDocument> BuildVisibleSelectionFilter(IReadOnlyCollection<string> excludedParkIds)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        List<string> normalizedExcludedIds = NormalizeParkIds(excludedParkIds);

        if (normalizedExcludedIds.Count > 0)
        {
            filter &= Builders<ParkDocument>.Filter.Nin(document => document.Id, normalizedExcludedIds);
        }

        return filter;
    }

    private static List<string> NormalizeParkIds(IEnumerable<string> parkIds)
    {
        return parkIds
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static double CreateRandomSortKey()
    {
        return Random.Shared.NextDouble();
    }

    private FilterDefinition<ParkDocument> BuildVisibilityFilter(bool includeHidden)
    {
        return includeHidden
            ? Builders<ParkDocument>.Filter.Empty
            : Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
    }
}
