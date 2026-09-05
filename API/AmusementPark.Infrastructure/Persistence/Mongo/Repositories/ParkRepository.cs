using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Parks.Contracts;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
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
    private const int ConditionalBulkMutationBatchSize = 200;

    private const int RandomVisibleFallbackHardLimit = 100;

    private readonly IMongoCollection<ParkDocument> collection;
    private readonly IRatingRankSnapshotCache ratingRankSnapshotCache;
    private readonly IRatingRankingSourceChangeCoordinator rankingSourceChangeCoordinator;

    public ParkRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IRatingRankSnapshotCache ratingRankSnapshotCache,
        IRatingRankingSourceChangeCoordinator rankingSourceChangeCoordinator)
    {
        this.collection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.ratingRankSnapshotCache = ratingRankSnapshotCache;
        this.rankingSourceChangeCoordinator = rankingSourceChangeCoordinator;
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

    public async Task<PagedResult<Park>> GetPageAsync(int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false, ParkAudienceClassificationFilter? audienceClassificationFilter = null)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates, closedFilter, audienceClassificationFilter);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Sort(ParkListOrdering.Build(sortField, sortDescending, includeHidden))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<long> CountAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden) & BuildClosedFilter(closedFilter);
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

    public Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(string? searchTerm, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        ParkSearchCriteria criteria = new ParkSearchCriteria(searchTerm, Array.Empty<string>(), Array.Empty<string>());
        return this.GetVisibleMapPointsAsync(criteria, closedFilter, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Park>> GetVisibleMapPointsAsync(ParkSearchCriteria criteria, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true)
            & Builders<ParkDocument>.Filter.Ne(document => document.Latitude, null)
            & Builders<ParkDocument>.Filter.Ne(document => document.Longitude, null)
            & this.BuildCriteriaFilter(criteria)
            & BuildClosedFilter(closedFilter);

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
            & BuildValidCoordinatesFilter()
            & BuildClosedFilter(ClosedEntityFilter.OpenOnly);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Project(BuildMapPointProjection())
            .SortBy(document => document.Name)
            .ThenBy(document => document.Id)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        return this.GetRandomVisibleAsync(limit, Array.Empty<string>(), closedFilter, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Park>> GetRandomVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        int safeLimit = Math.Min(limit, RandomVisibleFallbackHardLimit);
        FilterDefinition<ParkDocument> filter = this.BuildVisibleSelectionFilter(excludedParkIds, closedFilter);
        List<ParkDocument> documents = await this.LoadRandomVisibleWindowAsync(filter, safeLimit, cancellationToken);

        if (documents.Count == 0)
        {
            documents = await this.LoadRandomVisibleFallbackAsync(filter, safeLimit, cancellationToken);
        }

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetManualHomeFeaturedVisibleAsync(int limit, IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        FilterDefinition<ParkDocument> filter = this.BuildVisibleSelectionFilter(excludedParkIds, closedFilter)
            & Builders<ParkDocument>.Filter.Eq(document => document.IsFeaturedOnHome, true);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortBy(document => document.FeaturedHomeOrder)
            .ThenBy(document => document.Name)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetLatestVisibleAsync(int limit, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        FilterDefinition<ParkDocument> filter = this.BuildVisibleSelectionFilter(Array.Empty<string>(), closedFilter);
        List<ParkDocument> documents = await this.collection.Find(filter)
            .SortByDescending(document => document.UpdatedAt)
            .ThenByDescending(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<int> CountDistinctCountryCodesAsync(bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden)
            & BuildClosedFilter(closedFilter)
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
        return this.SearchAsync(criteria, page, pageSize, includeHidden, null, null, null, null, null, ClosedEntityFilter.OpenOnly, cancellationToken);
    }

    public async Task<PagedResult<Park>> SearchAsync(ParkSearchCriteria criteria, int page, int pageSize, bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, CancellationToken cancellationToken, ParkAdminSortField sortField = ParkAdminSortField.Default, bool sortDescending = false)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates, closedFilter)
            & this.BuildCriteriaFilter(criteria);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Sort(ParkListOrdering.Build(sortField, sortDescending, includeHidden))
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<Park>(
            documents.Select(document => document.ToDomain()).ToList(),
            page,
            pageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<Park>> SearchByLocationAsync(double latitude, double longitude, double radiusInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = BuildGeoJsonPoint(latitude, longitude);
        FilterDefinition<ParkDocument> filter = this.BuildNearLocationFilter(center, radiusInKilometers, includeHidden, closedFilter);

        List<ParkDocument> documents = await this.collection.Find(filter).ToListAsync(cancellationToken);
        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<Park>> GetNearestByLocationAsync(double latitude, double longitude, int limit, double? maxDistanceInKilometers, bool includeHidden, ClosedEntityFilter closedFilter, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<Park>();
        }

        GeoJsonPoint<GeoJson2DGeographicCoordinates> center = BuildGeoJsonPoint(latitude, longitude);
        FilterDefinition<ParkDocument> filter = this.BuildNearLocationFilter(center, maxDistanceInKilometers, includeHidden, closedFilter);

        List<ParkDocument> documents = await this.collection.Find(filter)
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(document => document.ToDomain()).ToList();
    }

    public async Task<Park> CreateAsync(Park park, CancellationToken cancellationToken)
    {
        RatingRankingMutationPreparation rankingPreparation =
            await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                Array.Empty<Park>(),
                new[] { park },
                cancellationToken);
        ParkDocument document = park.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;
        document.RandomSortKey = CreateRandomSortKey();

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);

        this.ratingRankSnapshotCache.Invalidate();
        await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
            rankingPreparation,
            sourceChanged: true,
            CancellationToken.None);
        return document.ToDomain();
    }

    public async Task<Park?> UpdateAsync(string parkId, Park park, CancellationToken cancellationToken)
    {
        while (true)
        {
            ParkDocument? existing = await this.collection.Find(document => document.Id == parkId)
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
            RatingRankingMutationPreparation rankingPreparation =
                await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                    new[] { existing.ToDomain() },
                    new[] { document.ToDomain() },
                    cancellationToken);

            ReplaceOneResult result = await this.collection.ReplaceOneAsync(
                BuildObservedRankingStateFilter(existing),
                document,
                cancellationToken: cancellationToken);

            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged: result.MatchedCount > 0,
                CancellationToken.None);
            if (result.MatchedCount == 0)
            {
                cancellationToken.ThrowIfCancellationRequested();
                continue;
            }

            this.ratingRankSnapshotCache.Invalidate();
            return document.ToDomain();
        }
    }

    public async Task<bool> DeleteAsync(string parkId, CancellationToken cancellationToken)
    {
        while (true)
        {
            ParkDocument? existing = await this.collection.Find(document => document.Id == parkId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                return false;
            }

            RatingRankingMutationPreparation rankingPreparation =
                await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                    new[] { existing.ToDomain() },
                    Array.Empty<Park>(),
                    cancellationToken);
            DeleteResult result = await this.collection.DeleteOneAsync(
                BuildObservedRankingStateFilter(existing),
                cancellationToken: cancellationToken);
            bool deleted = result.DeletedCount > 0;
            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged: deleted,
                CancellationToken.None);
            if (!deleted)
            {
                cancellationToken.ThrowIfCancellationRequested();
                continue;
            }

            this.ratingRankSnapshotCache.Invalidate();
            return true;
        }
    }

    internal static FilterDefinition<ParkDocument> BuildObservedRankingStateFilter(
        ParkDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return Builders<ParkDocument>.Filter.Eq(value => value.Id, document.Id)
            & Builders<ParkDocument>.Filter.Eq(value => value.IsVisible, document.IsVisible)
            & Builders<ParkDocument>.Filter.Eq(value => value.Status, document.Status)
            & Builders<ParkDocument>.Filter.Eq(value => value.Name, document.Name);
    }

    public async Task<Park?> UpdateVisibilityAsync(string parkId, bool isVisible, CancellationToken cancellationToken)
    {
        while (true)
        {
            ParkDocument? existing = await this.collection.Find(document => document.Id == parkId)
                .FirstOrDefaultAsync(cancellationToken);
            if (existing is null)
            {
                return null;
            }

            Park current = existing.ToDomain();
            current.IsVisible = isVisible;
            RatingRankingMutationPreparation rankingPreparation =
                await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                    new[] { existing.ToDomain() },
                    new[] { current },
                    cancellationToken);
            UpdateDefinition<ParkDocument> update = Builders<ParkDocument>.Update
                .Set(document => document.IsVisible, isVisible)
                .Set(document => document.UpdatedAt, DateTime.UtcNow);

            FindOneAndUpdateOptions<ParkDocument> options = new FindOneAndUpdateOptions<ParkDocument>
            {
                ReturnDocument = ReturnDocument.After,
            };

            ParkDocument? updated = await this.collection.FindOneAndUpdateAsync(
                BuildObservedRankingStateFilter(existing),
                update,
                options,
                cancellationToken);
            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                sourceChanged: updated is not null,
                CancellationToken.None);
            if (updated is null)
            {
                cancellationToken.ThrowIfCancellationRequested();
                continue;
            }

            this.ratingRankSnapshotCache.Invalidate();
            return updated.ToDomain();
        }
    }

    public async Task<int> UpdateBulkAdministrationAsync(IReadOnlyCollection<string> parkIds, bool? isVisible, AdminReviewStatus? adminReviewStatus, CancellationToken cancellationToken)
    {
        List<string> normalizedParkIds = NormalizeParkIds(parkIds);
        if (normalizedParkIds.Count == 0 || (!isVisible.HasValue && !adminReviewStatus.HasValue))
        {
            return 0;
        }

        AdminReviewStatus? normalizedAdminReviewStatus = adminReviewStatus.HasValue
            ? adminReviewStatus.Value.NormalizeForAdministration()
            : null;
        HashSet<string> pendingIds = normalizedParkIds.ToHashSet(StringComparer.Ordinal);
        HashSet<string> updatedIds = new HashSet<string>(StringComparer.Ordinal);
        bool rankingSourceChanged = false;
        while (pendingIds.Count > 0)
        {
            string[] batchIds = pendingIds.Take(ConditionalBulkMutationBatchSize).ToArray();
            List<ParkDocument> previousDocuments = await this.collection
                .Find(Builders<ParkDocument>.Filter.In(document => document.Id, batchIds))
                .ToListAsync(cancellationToken);
            RemoveMissingIds(pendingIds, batchIds, previousDocuments.Select(static document => document.Id));
            if (previousDocuments.Count == 0)
            {
                continue;
            }

            IReadOnlyCollection<Park> previousParks = previousDocuments
                .Select(static document => document.ToDomain())
                .ToArray();
            IReadOnlyCollection<Park> currentParks = previousDocuments
                .Select(document =>
                {
                    Park current = document.ToDomain();
                    if (isVisible.HasValue)
                    {
                        current.IsVisible = isVisible.Value;
                    }

                    return current;
                })
                .ToArray();
            RatingRankingMutationPreparation rankingPreparation =
                await this.rankingSourceChangeCoordinator.PrepareParkChangesAsync(
                    previousParks,
                    currentParks,
                    cancellationToken);
            UpdateDefinition<ParkDocument> update = BuildBulkAdministrationUpdate(
                isVisible,
                normalizedAdminReviewStatus,
                DateTime.UtcNow);
            FilterDefinition<ParkDocument> observedStatesFilter = Builders<ParkDocument>.Filter.Or(
                previousDocuments.Select(BuildObservedRankingStateFilter));
            UpdateResult result = await this.collection.UpdateManyAsync(
                observedStatesFilter,
                update,
                cancellationToken: cancellationToken);
            bool batchSourceChanged = result.ModifiedCount > 0;
            rankingSourceChanged |= batchSourceChanged;
            await this.rankingSourceChangeCoordinator.CompleteMutationAsync(
                rankingPreparation,
                batchSourceChanged,
                CancellationToken.None);

            List<ParkDocument> currentDocuments = await this.collection
                .Find(Builders<ParkDocument>.Filter.In(document => document.Id, batchIds))
                .ToListAsync(cancellationToken);
            RemoveMissingIds(pendingIds, batchIds, currentDocuments.Select(static document => document.Id));
            foreach (ParkDocument document in currentDocuments.Where(document =>
                         MatchesBulkAdministrationTarget(document, isVisible, normalizedAdminReviewStatus)))
            {
                pendingIds.Remove(document.Id);
                updatedIds.Add(document.Id);
            }
        }

        if (rankingSourceChanged && isVisible.HasValue)
        {
            this.ratingRankSnapshotCache.Invalidate();
        }

        return updatedIds.Count;
    }

    internal static bool MatchesBulkAdministrationTarget(
        ParkDocument document,
        bool? isVisible,
        AdminReviewStatus? adminReviewStatus)
    {
        ArgumentNullException.ThrowIfNull(document);
        return (!isVisible.HasValue || document.IsVisible == isVisible.Value)
            && (!adminReviewStatus.HasValue || document.AdminReviewStatus == adminReviewStatus.Value);
    }

    private static UpdateDefinition<ParkDocument> BuildBulkAdministrationUpdate(
        bool? isVisible,
        AdminReviewStatus? adminReviewStatus,
        DateTime updatedAtUtc)
    {
        UpdateDefinition<ParkDocument> update = Builders<ParkDocument>.Update
            .Set(document => document.UpdatedAt, updatedAtUtc);
        if (isVisible.HasValue)
        {
            update = update.Set(document => document.IsVisible, isVisible.Value);
        }

        if (adminReviewStatus.HasValue)
        {
            update = update
                .Set(document => document.AdminReviewStatus, adminReviewStatus.Value)
                .Set(document => document.AdminReviewPriority, adminReviewStatus.Value.ToAdminReviewPriority());
        }

        return update;
    }

    private static void RemoveMissingIds(
        ISet<string> pendingIds,
        IReadOnlyCollection<string> requestedIds,
        IEnumerable<string> foundIds)
    {
        HashSet<string> found = foundIds.ToHashSet(StringComparer.Ordinal);
        foreach (string missingId in requestedIds.Where(id => !found.Contains(id)))
        {
            pendingIds.Remove(missingId);
        }
    }

    public async Task<IReadOnlyCollection<string>> GetAdministrationIdsAsync(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, CancellationToken cancellationToken, ParkAudienceClassificationFilter? audienceClassificationFilter = null)
    {
        FilterDefinition<ParkDocument> filter = this.BuildAdminListFilter(includeHidden, isVisible, adminReviewStatus, type, countryCode, hasValidCoordinates, ClosedEntityFilter.All, audienceClassificationFilter);

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
            AudienceClassification = document.AudienceClassification,
            Status = document.Status,
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

    private FilterDefinition<ParkDocument> BuildNearLocationFilter(GeoJsonPoint<GeoJson2DGeographicCoordinates> center, double? radiusInKilometers, bool includeHidden, ClosedEntityFilter closedFilter)
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

        return filter & BuildClosedFilter(closedFilter);
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

        FilterDefinition<ParkDocument>? audienceClassificationFilter = BuildAudienceClassificationFilter(criteria.AudienceClassificationFilter);
        if (audienceClassificationFilter is not null)
        {
            filter &= audienceClassificationFilter;
        }

        if (criteria.Status.HasValue)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.Status, criteria.Status.Value);
        }

        FilterDefinition<ParkDocument>? searchFilter = BuildSearchTermFilter(criteria);
        if (searchFilter is not null)
        {
            filter &= searchFilter;
        }

        return filter;
    }

    internal static FilterDefinition<ParkDocument>? BuildSearchTermFilter(ParkSearchCriteria criteria)
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
            filters.Add(Builders<ParkDocument>.Filter.Regex(document => document.PostalCode, expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex("descriptions.value", expression));
            filters.Add(Builders<ParkDocument>.Filter.Regex("type", expression));

            string compactTypeSearch = CompactTypeSearchTerm(normalizedTerm);
            if (!string.Equals(compactTypeSearch, normalizedTerm, StringComparison.OrdinalIgnoreCase))
            {
                filters.Add(Builders<ParkDocument>.Filter.Regex(
                    "type",
                    new BsonRegularExpression(Regex.Escape(compactTypeSearch), "i")));
            }
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

    private static string CompactTypeSearchTerm(string searchTerm)
    {
        return searchTerm
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace("-", string.Empty, StringComparison.Ordinal);
    }

    private FilterDefinition<ParkDocument> BuildAdminListFilter(bool includeHidden, bool? isVisible, AdminReviewStatus? adminReviewStatus, ParkType? type, string? countryCode, bool? hasValidCoordinates, ClosedEntityFilter closedFilter, ParkAudienceClassificationFilter? audienceClassificationFilter = null)
    {
        FilterDefinition<ParkDocument> filter = this.BuildVisibilityFilter(includeHidden) & BuildClosedFilter(closedFilter);

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

        FilterDefinition<ParkDocument>? audienceFilter = BuildAudienceClassificationFilter(audienceClassificationFilter);
        if (audienceFilter is not null)
        {
            filter &= audienceFilter;
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

    private static FilterDefinition<ParkDocument>? BuildAudienceClassificationFilter(ParkAudienceClassificationFilter? audienceClassificationFilter)
    {
        if (!audienceClassificationFilter.HasValue)
        {
            return null;
        }

        if (audienceClassificationFilter.Value == ParkAudienceClassificationFilter.Unspecified)
        {
            return Builders<ParkDocument>.Filter.Or(
                Builders<ParkDocument>.Filter.Exists(document => document.AudienceClassification, false),
                Builders<ParkDocument>.Filter.Eq(document => document.AudienceClassification, null));
        }

        ParkAudienceClassification audienceClassification = audienceClassificationFilter.Value switch
        {
            ParkAudienceClassificationFilter.International => ParkAudienceClassification.International,
            ParkAudienceClassificationFilter.National => ParkAudienceClassification.National,
            ParkAudienceClassificationFilter.Regional => ParkAudienceClassification.Regional,
            ParkAudienceClassificationFilter.Local => ParkAudienceClassification.Local,
            _ => throw new ArgumentOutOfRangeException(nameof(audienceClassificationFilter), audienceClassificationFilter, "Unsupported audience classification filter."),
        };

        return Builders<ParkDocument>.Filter.Eq(document => document.AudienceClassification, audienceClassification);
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

    private FilterDefinition<ParkDocument> BuildVisibleSelectionFilter(IReadOnlyCollection<string> excludedParkIds, ClosedEntityFilter closedFilter)
    {
        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true)
            & BuildClosedFilter(closedFilter);
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

    internal static FilterDefinition<ParkDocument> BuildClosedFilter(ClosedEntityFilter closedFilter)
    {
        return closedFilter switch
        {
            ClosedEntityFilter.All => Builders<ParkDocument>.Filter.Empty,
            ClosedEntityFilter.ClosedOnly => Builders<ParkDocument>.Filter.Eq(document => document.Status, ParkStatus.ClosedDefinitively),
            _ => Builders<ParkDocument>.Filter.Eq(document => document.Status, ParkStatus.Operating),
        };
    }
}
