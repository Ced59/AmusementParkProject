using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RatingRepository : IRatingRepository
{
    private const int RankingCandidateHardLimit = 5000;
    private const int UserRatingSearchHardLimit = 1000;

    private readonly IMongoCollection<UserRatingDocument> userRatingsCollection;
    private readonly IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection;
    private readonly IMongoCollection<ParkDocument> parksCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;
    private readonly RatingAggregateSynchronizer aggregateSynchronizer;
    private readonly RatingAggregateSourceReader aggregateSourceReader;

    public RatingRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.userRatingsCollection = database.GetCollection<UserRatingDocument>(settings.UserRatingsCollectionName);
        this.ratingAggregatesCollection = database.GetCollection<RatingAggregateDocument>(settings.RatingAggregatesCollectionName);
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
        this.aggregateSynchronizer = new RatingAggregateSynchronizer(
            this.userRatingsCollection,
            this.ratingAggregatesCollection);
        this.aggregateSourceReader = new RatingAggregateSourceReader(this.userRatingsCollection);
    }

    public async Task<UserRating?> GetUserRatingAsync(string userId, RatingTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = BuildUserTargetFilter(userId, targetType, targetId);
        UserRatingDocument? document = await this.userRatingsCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserRatingMutationResult> UpsertUserRatingAndRecalculateAggregateAsync(
        UserRating rating,
        RatingAggregateTarget aggregateTarget,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rating);
        ArgumentNullException.ThrowIfNull(aggregateTarget);

        UserRating upsertedRating = await this.UpsertUserRatingDocumentAsync(rating, cancellationToken);
        RatingAggregate? aggregate = await this.aggregateSynchronizer.RecalculateAsync(aggregateTarget, cancellationToken);
        return new UserRatingMutationResult(upsertedRating, aggregate);
    }

    public async Task<UserRatingDeletionResult> DeleteUserRatingAndRecalculateAggregateAsync(
        string userId,
        RatingTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = BuildUserTargetFilter(userId, targetType, targetId);
        UserRatingDocument? document = await this.userRatingsCollection.FindOneAndDeleteAsync(
            filter,
            cancellationToken: cancellationToken);
        if (document is null)
        {
            RatingAggregate? currentAggregate = await this.GetAggregateAsync(targetType, targetId, cancellationToken);
            return new UserRatingDeletionResult(null, currentAggregate);
        }

        UserRating deletedRating = document.ToDomain();
        RatingAggregateTarget aggregateTarget = new RatingAggregateTarget(
            deletedRating.TargetType,
            deletedRating.TargetId,
            deletedRating.ParkId,
            deletedRating.ParkItemCategory,
            deletedRating.ParkItemType);
        RatingAggregate? aggregate = await this.aggregateSynchronizer.RecalculateAsync(
            aggregateTarget,
            cancellationToken);
        return new UserRatingDeletionResult(deletedRating, aggregate);
    }

    public async Task<RatingAggregate?> GetAggregateAsync(RatingTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        FilterDefinition<RatingAggregateDocument> filter = BuildAggregateTargetFilter(targetType, targetId)
            & Builders<RatingAggregateDocument>.Filter.Gt(document => document.RatingCount, 0);
        RatingAggregateDocument? document = await this.ratingAggregatesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        RatingAggregate aggregate = document.ToDomain();
        IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts = await this.aggregateSourceReader.ReadAsync(
            new[] { new RatingAggregateSourceTarget(targetType, targetId) },
            cancellationToken);
        aggregate.SourceIntegrityIsValid = RatingAggregateSourceReader.TryVerifyAndHydrateProjection(
            aggregate,
            sourceFacts.SingleOrDefault());
        return aggregate;
    }

    private async Task<UserRating> UpsertUserRatingDocumentAsync(UserRating rating, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(rating);

        DateTime nowUtc = DateTime.UtcNow;
        string documentId = string.IsNullOrWhiteSpace(rating.Id) ? Guid.NewGuid().ToString("N") : rating.Id;
        FilterDefinition<UserRatingDocument> filter = BuildUserTargetFilter(rating.UserId, rating.TargetType, rating.TargetId);
        UpdateDefinition<UserRatingDocument> update = Builders<UserRatingDocument>.Update
            .SetOnInsert(document => document.Id, documentId)
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .Set(document => document.UserId, rating.UserId.Trim())
            .Set(document => document.TargetType, rating.TargetType)
            .Set(document => document.TargetId, rating.TargetId.Trim())
            .Set(document => document.ParkId, rating.ParkId.Trim())
            .Set(document => document.ParkItemCategory, rating.ParkItemCategory)
            .Set(document => document.ParkItemType, rating.ParkItemType)
            .Set(document => document.Value, rating.Value)
            .Set(document => document.UpdatedAt, nowUtc);

        FindOneAndUpdateOptions<UserRatingDocument> options = new FindOneAndUpdateOptions<UserRatingDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        UserRatingDocument? document = await this.userRatingsCollection.FindOneAndUpdateAsync(filter, update, options, cancellationToken);
        if (document is null)
        {
            document = await this.userRatingsCollection.Find(filter).FirstAsync(cancellationToken);
        }

        return document.ToDomain();
    }

    public async Task<PagedResult<UserRatingListItemResult>> GetUserRatingsAsync(string userId, int page, int pageSize, string? parkSearch, CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = Builders<UserRatingDocument>.Filter.Eq(document => document.UserId, userId);

        if (!string.IsNullOrWhiteSpace(parkSearch))
        {
            List<UserRatingDocument> searchDocuments = await this.userRatingsCollection.Find(filter)
                .SortByDescending(document => document.UpdatedAt)
                .Limit(UserRatingSearchHardLimit)
                .ToListAsync(cancellationToken);

            IReadOnlyCollection<UserRatingListItemResult> enrichedRatings = await this.EnrichUserRatingsAsync(searchDocuments, cancellationToken);
            IReadOnlyCollection<UserRatingListItemResult> searchItems = BuildUserRatingSearchWindow(enrichedRatings, parkSearch.Trim(), pageSize);
            return new PagedResult<UserRatingListItemResult>(searchItems, 1, pageSize, searchItems.Count);
        }

        long totalItems = await this.userRatingsCollection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<UserRatingDocument> documents = await this.userRatingsCollection.Find(filter)
            .SortByDescending(document => document.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        IReadOnlyCollection<UserRatingListItemResult> items = await this.EnrichUserRatingsAsync(documents, cancellationToken);
        return new PagedResult<UserRatingListItemResult>(items, page, pageSize, totalItems);
    }

    private async Task<IReadOnlyCollection<UserRatingListItemResult>> EnrichUserRatingsAsync(IReadOnlyCollection<UserRatingDocument> documents, CancellationToken cancellationToken)
    {
        List<string> parkTargetIds = documents
            .Where(static document => document.TargetType == RatingTargetType.Park)
            .Select(static document => document.TargetId)
            .ToList();
        IReadOnlyDictionary<string, ParkItemDocument> parkItems = await this.LoadParkItemsAsync(
            documents.Where(static document => document.TargetType == RatingTargetType.ParkItem).Select(static document => document.TargetId),
            false,
            cancellationToken);
        Dictionary<string, ParkDocument> parks = await this.LoadParkDocumentsAsync(
            documents
                .Select(static document => document.ParkId)
                .Concat(parkTargetIds)
                .Concat(parkItems.Values.Select(static parkItem => parkItem.ParkId)),
            false,
            cancellationToken);
        IReadOnlyDictionary<string, RatingAggregate> aggregates = await this.LoadAggregatesAsync(documents, cancellationToken);

        List<UserRatingListItemResult> items = documents.Select(document =>
        {
            string key = BuildTargetKey(document.TargetType, document.TargetId);
            aggregates.TryGetValue(key, out RatingAggregate? aggregate);
            bool targetCanReceiveVisitorRatings = CanTargetReceiveVisitorRatings(document, parks, parkItems);
            RatingSummaryResult summary = ToSummary(
                document.TargetType,
                document.TargetId,
                aggregate,
                targetCanReceiveVisitorRatings);
            string? parkName = parks.TryGetValue(document.ParkId, out ParkDocument? park)
                ? park.Name?.Trim() ?? park.Id
                : null;
            string targetName = ResolveTargetName(document, parkName, parkItems);

            return new UserRatingListItemResult(
                document.Id,
                document.TargetType,
                document.TargetId,
                targetName,
                document.ParkId,
                parkName,
                document.ParkItemCategory,
                document.ParkItemType,
                document.Value,
                document.UpdatedAt,
                summary);
        }).ToList();

        return items;
    }

    public async Task<UserRatingStatsResult> GetUserRatingStatsAsync(string userId, CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = Builders<UserRatingDocument>.Filter.Eq(document => document.UserId, userId);
        List<UserRatingDocument> documents = await this.userRatingsCollection.Find(filter).ToListAsync(cancellationToken);
        return await this.BuildUserRatingStatsAsync(documents, false, cancellationToken);
    }

    public async Task<UserRatingStatsResult> GetVisibleUserRatingStatsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = Builders<UserRatingDocument>.Filter.Eq(
            document => document.UserId,
            userId.Trim());
        List<UserRatingDocument> documents = await this.userRatingsCollection.Find(filter).ToListAsync(cancellationToken);
        IReadOnlyCollection<UserRatingDocument> visibleDocuments = await this.FilterVisibleUserRatingsAsync(
            documents,
            cancellationToken);
        return await this.BuildUserRatingStatsAsync(visibleDocuments, true, cancellationToken);
    }

    private async Task<UserRatingStatsResult> BuildUserRatingStatsAsync(
        IReadOnlyCollection<UserRatingDocument> documents,
        bool visibleParkNamesOnly,
        CancellationToken cancellationToken)
    {

        if (documents.Count == 0)
        {
            return new UserRatingStatsResult(0, 0d, 0d, 0d, Array.Empty<UserRatingStatBucketResult>(), Array.Empty<UserRatingStatBucketResult>(), Array.Empty<UserRatingStatBucketResult>());
        }

        IReadOnlyDictionary<string, string> parkNames = await this.LoadParkNamesAsync(
            documents.Select(static document => document.ParkId),
            visibleParkNamesOnly,
            cancellationToken);
        List<UserRatingStatBucketResult> byPark = documents
            .Where(static document => !string.IsNullOrWhiteSpace(document.ParkId))
            .GroupBy(static document => document.ParkId, StringComparer.Ordinal)
            .Select(group =>
            {
                string label = parkNames.TryGetValue(group.Key, out string? parkName) ? parkName : group.Key;
                return new UserRatingStatBucketResult(group.Key, label, group.LongCount(), group.Average(static document => document.Value));
            })
            .OrderByDescending(static bucket => bucket.Count)
            .ThenByDescending(static bucket => bucket.AverageRating)
            .Take(8)
            .ToList();

        List<UserRatingStatBucketResult> byTargetType = documents
            .GroupBy(static document => document.TargetType)
            .Select(static group => new UserRatingStatBucketResult(group.Key.ToString(), group.Key.ToString(), group.LongCount(), group.Average(static document => document.Value)))
            .OrderByDescending(static bucket => bucket.Count)
            .ThenBy(static bucket => bucket.Key, StringComparer.Ordinal)
            .ToList();

        List<UserRatingStatBucketResult> byParkItemCategory = documents
            .Where(static document => document.ParkItemCategory.HasValue)
            .GroupBy(static document => document.ParkItemCategory!.Value)
            .Select(static group => new UserRatingStatBucketResult(group.Key.ToString(), group.Key.ToString(), group.LongCount(), group.Average(static document => document.Value)))
            .OrderByDescending(static bucket => bucket.Count)
            .ThenBy(static bucket => bucket.Key, StringComparer.Ordinal)
            .ToList();

        return new UserRatingStatsResult(
            documents.LongCount(),
            documents.Average(static document => document.Value),
            documents.Max(static document => document.Value),
            documents.Min(static document => document.Value),
            byPark,
            byTargetType,
            byParkItemCategory);
    }

    public async Task<RatingRankingSourceBatch> GetVisibleRankingSourcesAsync(
        ParkItemCategory? parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        List<RatingAggregateDocument> parkDocuments = await this.ratingAggregatesCollection.Find(BuildParkRankingParkFilter())
            .Sort(BuildRankingSort())
            .Limit(RankingCandidateHardLimit + 1)
            .ToListAsync(cancellationToken);
        List<RatingAggregateDocument> parkItemDocuments = await this.ratingAggregatesCollection.Find(BuildParkRankingItemFilter(parkItemCategory))
            .Sort(BuildRankingSort())
            .Limit(effectiveMaxItems + 1)
            .ToListAsync(cancellationToken);
        bool isTruncated = IsVisibleRankingSourceSetTruncated(
            parkDocuments.Count,
            parkItemDocuments.Count,
            effectiveMaxItems);
        List<RatingAggregateDocument> candidateDocuments = parkDocuments
            .Take(RankingCandidateHardLimit)
            .Concat(parkItemDocuments.Take(effectiveMaxItems))
            .ToList();

        if (candidateDocuments.Count == 0)
        {
            return new RatingRankingSourceBatch(
                Array.Empty<RatingRankingItemResult>(),
                isTruncated);
        }

        IReadOnlyCollection<RatingRankingItemResult> sources = await this.EnrichVisibleRankingSourcesAsync(
            candidateDocuments,
            cancellationToken);
        return new RatingRankingSourceBatch(sources, isTruncated);
    }

    public async Task<IReadOnlyCollection<RatingRankingItemResult>> GetVisibleParkItemRankingSourcesAsync(
        ParkItemCategory parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        List<RatingAggregateDocument> documents = await this.ratingAggregatesCollection.Find(
                BuildParkRankingItemFilter(parkItemCategory))
            .Sort(BuildRankingSort())
            .Limit(effectiveMaxItems)
            .ToListAsync(cancellationToken);

        if (documents.Count == 0)
        {
            return Array.Empty<RatingRankingItemResult>();
        }

        return await this.EnrichVisibleRankingSourcesAsync(documents, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserRatingListItemResult>> GetUserRankingSourcesAsync(
        string userId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        FilterDefinition<UserRatingDocument> filter = Builders<UserRatingDocument>.Filter.Eq(
            document => document.UserId,
            userId.Trim());
        List<UserRatingDocument> documents = await this.userRatingsCollection.Find(filter)
            .SortByDescending(document => document.Value)
            .ThenBy(document => document.TargetId)
            .Limit(effectiveMaxItems)
            .ToListAsync(cancellationToken);

        return await this.EnrichUserRatingsAsync(documents, cancellationToken);
    }

    public async Task<IReadOnlyCollection<UserRatingListItemResult>> GetVisibleUserRankingSourcesAsync(
        string userId,
        int maxItems,
        CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        FilterDefinition<UserRatingDocument> filter = Builders<UserRatingDocument>.Filter.Eq(
            document => document.UserId,
            userId.Trim());
        List<UserRatingDocument> documents = await this.userRatingsCollection.Find(filter)
            .SortByDescending(document => document.Value)
            .ThenBy(document => document.TargetId)
            .Limit(effectiveMaxItems)
            .ToListAsync(cancellationToken);
        IReadOnlyCollection<UserRatingDocument> visibleDocuments = await this.FilterVisibleUserRatingsAsync(
            documents,
            cancellationToken);

        return await this.EnrichUserRatingsAsync(visibleDocuments, cancellationToken);
    }

    private async Task<IReadOnlyCollection<UserRatingDocument>> FilterVisibleUserRatingsAsync(
        IReadOnlyCollection<UserRatingDocument> documents,
        CancellationToken cancellationToken)
    {
        List<string> parkTargetIds = documents
            .Where(static document => document.TargetType == RatingTargetType.Park)
            .Select(static document => document.TargetId)
            .ToList();
        List<string> parkIds = documents
            .Select(static document => document.ParkId)
            .Concat(parkTargetIds)
            .ToList();
        Dictionary<string, ParkDocument> visibleParks = await this.LoadParkDocumentsAsync(
            parkIds,
            true,
            cancellationToken);
        Dictionary<string, ParkItemDocument> visibleItems = await this.LoadParkItemDocumentsAsync(
            documents
                .Where(static document => document.TargetType == RatingTargetType.ParkItem)
                .Select(static document => document.TargetId),
            true,
            cancellationToken);
        List<UserRatingDocument> visibleRatings = new List<UserRatingDocument>();

        foreach (UserRatingDocument document in documents)
        {
            if (IsPublicUserRatingSource(document, visibleParks, visibleItems))
            {
                visibleRatings.Add(document);
            }
        }

        return visibleRatings;
    }

    internal static bool IsPublicUserRatingSource(
        UserRatingDocument document,
        IReadOnlyDictionary<string, ParkDocument> visibleParks,
        IReadOnlyDictionary<string, ParkItemDocument> visibleItems)
    {
        if (document.TargetType == RatingTargetType.Park)
        {
            return visibleParks.TryGetValue(document.TargetId, out ParkDocument? park)
                && park.IsVisible
                && park.Status.CanAppearInCurrentRatingRankings();
        }

        if (!visibleItems.TryGetValue(document.TargetId, out ParkItemDocument? parkItem)
            || !parkItem.IsVisible
            || !visibleParks.TryGetValue(parkItem.ParkId, out ParkDocument? parentPark)
            || !parentPark.IsVisible)
        {
            return false;
        }

        return parentPark.Status.CanAppearInCurrentRatingRankings()
            && ParkItemStatusNormalizer.CanAppearInCurrentRatingRankings(
                parkItem.Category,
                parkItem.AttractionDetails?.Status);
    }

    private async Task<IReadOnlyCollection<RatingRankingItemResult>> EnrichVisibleRankingSourcesAsync(IReadOnlyCollection<RatingAggregateDocument> documents, CancellationToken cancellationToken)
    {
        List<string> parkTargetIds = documents
            .Where(static document => document.TargetType == RatingTargetType.Park)
            .Select(static document => document.TargetId)
            .ToList();
        List<string> parkIds = documents.Select(static document => document.ParkId).Concat(parkTargetIds).ToList();
        Dictionary<string, ParkDocument> visibleParks = await this.LoadParkDocumentsAsync(parkIds, true, cancellationToken);
        Dictionary<string, ParkItemDocument> visibleItems = await this.LoadParkItemDocumentsAsync(
            documents.Where(static document => document.TargetType == RatingTargetType.ParkItem).Select(static document => document.TargetId),
            true,
            cancellationToken);

        List<RatingRankingItemResult> items = new List<RatingRankingItemResult>();
        foreach (RatingAggregateDocument document in documents)
        {
            if (document.TargetType == RatingTargetType.Park)
            {
                if (!visibleParks.TryGetValue(document.TargetId, out ParkDocument? park))
                {
                    continue;
                }

                if (!park.Status.CanAppearInCurrentRatingRankings())
                {
                    continue;
                }

                items.Add(new RatingRankingItemResult(
                    document.TargetType,
                    document.TargetId,
                    park.Name?.Trim() ?? document.TargetId,
                    park.Id,
                    park.Name?.Trim(),
                    null,
                    null,
                    document.RatingCount,
                    document.RatingSum,
                    document.AverageRating,
                    document.BayesianScore)
                {
                    UniqueContributorCount = document.UniqueContributorCount,
                    AggregateIntegrityIsValid = IsAggregateCalculationCurrent(document),
                });
                continue;
            }

            if (!visibleItems.TryGetValue(document.TargetId, out ParkItemDocument? parkItem))
            {
                continue;
            }

            if (!visibleParks.TryGetValue(parkItem.ParkId, out ParkDocument? parentPark))
            {
                continue;
            }

            if (!parentPark.Status.CanAppearInCurrentRatingRankings()
                || !ParkItemStatusNormalizer.CanAppearInCurrentRatingRankings(
                    parkItem.Category,
                    parkItem.AttractionDetails?.Status))
            {
                continue;
            }

            items.Add(new RatingRankingItemResult(
                document.TargetType,
                document.TargetId,
                parkItem.Name.Trim(),
                parkItem.ParkId,
                parentPark.Name?.Trim(),
                parkItem.Category,
                parkItem.Type,
                document.RatingCount,
                document.RatingSum,
                document.AverageRating,
                document.BayesianScore)
            {
                UniqueContributorCount = document.UniqueContributorCount,
                AggregateIntegrityIsValid = IsAggregateCalculationCurrent(document),
            });
        }

        return items;
    }

    private async Task<IReadOnlyDictionary<string, RatingAggregate>> LoadAggregatesAsync(IReadOnlyCollection<UserRatingDocument> ratings, CancellationToken cancellationToken)
    {
        List<FilterDefinition<RatingAggregateDocument>> filters = ratings
            .GroupBy(static rating => rating.TargetType)
            .Select(group =>
            {
                List<string> targetIds = NormalizeIds(group.Select(static rating => rating.TargetId));
                return Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, group.Key)
                    & Builders<RatingAggregateDocument>.Filter.In(document => document.TargetId, targetIds);
            })
            .ToList();

        if (filters.Count == 0)
        {
            return new Dictionary<string, RatingAggregate>(StringComparer.Ordinal);
        }

        FilterDefinition<RatingAggregateDocument> aggregateFilter =
            Builders<RatingAggregateDocument>.Filter.Or(filters)
            & Builders<RatingAggregateDocument>.Filter.Gt(document => document.RatingCount, 0);
        List<RatingAggregateDocument> documents = await this.ratingAggregatesCollection.Find(aggregateFilter).ToListAsync(cancellationToken);
        IReadOnlyCollection<RatingAggregateSourceFact> sourceFacts = await this.aggregateSourceReader.ReadAsync(
            documents.Select(static document => new RatingAggregateSourceTarget(
                    document.TargetType,
                    document.TargetId))
                .ToList(),
            cancellationToken);
        IReadOnlyDictionary<string, RatingAggregateSourceFact> sourceFactsByTarget = sourceFacts
            .GroupBy(
                static fact => BuildTargetKey(fact.TargetType, fact.TargetId),
                StringComparer.Ordinal)
            .Where(static group => group.Count() == 1)
            .ToDictionary(static group => group.Key, static group => group.Single(), StringComparer.Ordinal);
        Dictionary<string, RatingAggregate> aggregates = new Dictionary<string, RatingAggregate>(StringComparer.Ordinal);
        foreach (RatingAggregateDocument document in documents)
        {
            string key = BuildTargetKey(document.TargetType, document.TargetId);
            RatingAggregate aggregate = document.ToDomain();
            sourceFactsByTarget.TryGetValue(key, out RatingAggregateSourceFact? sourceFact);
            aggregate.SourceIntegrityIsValid = RatingAggregateSourceReader.TryVerifyAndHydrateProjection(
                aggregate,
                sourceFact);
            aggregates[key] = aggregate;
        }

        return aggregates;
    }

    private async Task<IReadOnlyDictionary<string, string>> LoadParkNamesAsync(IEnumerable<string> parkIds, bool visibleOnly, CancellationToken cancellationToken)
    {
        Dictionary<string, ParkDocument> documents = await this.LoadParkDocumentsAsync(parkIds, visibleOnly, cancellationToken);
        return documents.ToDictionary(
            static pair => pair.Key,
            static pair => pair.Value.Name?.Trim() ?? pair.Key,
            StringComparer.Ordinal);
    }

    private async Task<Dictionary<string, ParkDocument>> LoadParkDocumentsAsync(IEnumerable<string> parkIds, bool visibleOnly, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(parkIds);
        if (normalizedIds.Count == 0)
        {
            return new Dictionary<string, ParkDocument>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkDocument> filter = Builders<ParkDocument>.Filter.In(document => document.Id, normalizedIds);
        if (visibleOnly)
        {
            filter &= Builders<ParkDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkDocument> documents = await this.parksCollection.Find(filter).ToListAsync(cancellationToken);
        return documents.ToDictionary(static document => document.Id, StringComparer.Ordinal);
    }

    private async Task<IReadOnlyDictionary<string, ParkItemDocument>> LoadParkItemsAsync(IEnumerable<string> parkItemIds, bool visibleOnly, CancellationToken cancellationToken)
    {
        Dictionary<string, ParkItemDocument> documents = await this.LoadParkItemDocumentsAsync(parkItemIds, visibleOnly, cancellationToken);
        return documents;
    }

    private async Task<Dictionary<string, ParkItemDocument>> LoadParkItemDocumentsAsync(IEnumerable<string> parkItemIds, bool visibleOnly, CancellationToken cancellationToken)
    {
        List<string> normalizedIds = NormalizeIds(parkItemIds);
        if (normalizedIds.Count == 0)
        {
            return new Dictionary<string, ParkItemDocument>(StringComparer.Ordinal);
        }

        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(document => document.Id, normalizedIds);
        if (visibleOnly)
        {
            filter &= Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<ParkItemDocument> documents = await this.parkItemsCollection.Find(filter).ToListAsync(cancellationToken);
        return documents.ToDictionary(static document => document.Id, StringComparer.Ordinal);
    }

    private static FilterDefinition<RatingAggregateDocument> BuildParkRankingParkFilter()
    {
        FilterDefinition<RatingAggregateDocument> filter = Builders<RatingAggregateDocument>.Filter.Gt(document => document.RatingCount, 0);
        FilterDefinition<RatingAggregateDocument> parkFilter = Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, RatingTargetType.Park);
        return filter & parkFilter;
    }

    private static FilterDefinition<RatingAggregateDocument> BuildParkRankingItemFilter(ParkItemCategory? parkItemCategory)
    {
        FilterDefinition<RatingAggregateDocument> filter = Builders<RatingAggregateDocument>.Filter.Gt(document => document.RatingCount, 0);
        FilterDefinition<RatingAggregateDocument> parkItemFilter = Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, RatingTargetType.ParkItem);

        if (parkItemCategory.HasValue)
        {
            parkItemFilter &= Builders<RatingAggregateDocument>.Filter.Eq(document => document.ParkItemCategory, parkItemCategory.Value);
        }

        return filter & parkItemFilter;
    }

    internal static IReadOnlyCollection<UserRatingListItemResult> BuildUserRatingSearchWindow(
        IReadOnlyCollection<UserRatingListItemResult> ratings,
        string parkSearch,
        int pageSize)
    {
        if (ratings.Count == 0)
        {
            return Array.Empty<UserRatingListItemResult>();
        }

        List<IGrouping<string, UserRatingListItemResult>> groups = ratings
            .Where(static rating => !string.IsNullOrWhiteSpace(rating.ParkId))
            .GroupBy(static rating => rating.ParkId, StringComparer.Ordinal)
            .OrderByDescending(static group => group.Average(static rating => rating.Value))
            .ThenByDescending(static group => group.Count())
            .ThenBy(static group => group.First().ParkName ?? group.Key, StringComparer.OrdinalIgnoreCase)
            .ToList();

        int matchIndex = groups.FindIndex(group => (group.First().ParkName ?? group.Key).Contains(parkSearch, StringComparison.OrdinalIgnoreCase));
        if (matchIndex < 0)
        {
            return Array.Empty<UserRatingListItemResult>();
        }

        const int contextSize = 5;
        int startIndex = Math.Max(0, matchIndex - contextSize);
        int endIndex = Math.Min(groups.Count - 1, matchIndex + contextSize);
        IGrouping<string, UserRatingListItemResult> matchingGroup = groups[matchIndex];

        List<UserRatingListItemResult> matchingItems = OrderUserRatingSearchGroup(matchingGroup)
            .Take(pageSize)
            .ToList();
        if (matchingItems.Count >= pageSize)
        {
            return matchingItems;
        }

        List<UserRatingListItemResult> contextItems = groups
            .Skip(startIndex)
            .Take(endIndex - startIndex + 1)
            .Where(group => !string.Equals(group.Key, matchingGroup.Key, StringComparison.Ordinal))
            .SelectMany(static group => OrderUserRatingSearchGroup(group))
            .Take(pageSize - matchingItems.Count)
            .ToList();

        return matchingItems.Concat(contextItems).ToList();
    }

    private static IOrderedEnumerable<UserRatingListItemResult> OrderUserRatingSearchGroup(IEnumerable<UserRatingListItemResult> group)
    {
        return group
            .OrderByDescending(static rating => rating.Value)
            .ThenBy(static rating => rating.TargetName, StringComparer.OrdinalIgnoreCase);
    }

    private static SortDefinition<RatingAggregateDocument> BuildRankingSort()
    {
        return Builders<RatingAggregateDocument>.Sort
            .Descending(document => document.BayesianScore)
            .Descending(document => document.RatingCount)
            .Descending(document => document.AverageRating)
            .Ascending(document => document.TargetType)
            .Ascending(document => document.TargetId);
    }

    internal static bool CanTargetReceiveVisitorRatings(
        UserRatingDocument document,
        IReadOnlyDictionary<string, ParkDocument> parks,
        IReadOnlyDictionary<string, ParkItemDocument> parkItems)
    {
        if (document.TargetType == RatingTargetType.Park)
        {
            return parks.TryGetValue(document.TargetId, out ParkDocument? park)
                && park.Status.CanReceiveVisitorRatings();
        }

        return parkItems.TryGetValue(document.TargetId, out ParkItemDocument? parkItem)
            && parks.TryGetValue(parkItem.ParkId, out ParkDocument? parentPark)
            && parentPark.Status.CanReceiveVisitorRatings()
            && ParkItemStatusNormalizer.CanReceiveVisitorRatings(
                parkItem.Category,
                parkItem.AttractionDetails?.Status);
    }

    internal static bool IsVisibleRankingSourceSetTruncated(
        int parkDocumentCount,
        int parkItemDocumentCount,
        int parkItemLimit)
    {
        return parkDocumentCount > RankingCandidateHardLimit
            || parkItemDocumentCount > parkItemLimit;
    }

    private static bool IsAggregateCalculationCurrent(RatingAggregateDocument document)
    {
        return RatingAggregate.IsCalculationCurrentForVersions(
            document.MutationVersion,
            document.CalculatedVersion);
    }

    private static RatingSummaryResult ToSummary(
        RatingTargetType targetType,
        string targetId,
        RatingAggregate? aggregate,
        bool targetCanReceiveVisitorRatings)
    {
        return RatingResultFactory.CreateSummary(
            targetType,
            targetId,
            aggregate,
            targetCanReceiveVisitorRatings,
            aggregateIntegrityIsValid: aggregate is null ? false : null);
    }

    private static string ResolveTargetName(UserRatingDocument document, string? parkName, IReadOnlyDictionary<string, ParkItemDocument> parkItems)
    {
        if (document.TargetType == RatingTargetType.Park)
        {
            return parkName ?? document.TargetId;
        }

        if (parkItems.TryGetValue(document.TargetId, out ParkItemDocument? parkItem))
        {
            return parkItem.Name.Trim();
        }

        return document.TargetId;
    }

    private static FilterDefinition<UserRatingDocument> BuildUserTargetFilter(string userId, RatingTargetType targetType, string targetId)
    {
        return Builders<UserRatingDocument>.Filter.Eq(document => document.UserId, userId.Trim())
            & BuildUserRatingTargetFilter(targetType, targetId);
    }

    private static FilterDefinition<UserRatingDocument> BuildUserRatingTargetFilter(RatingTargetType targetType, string targetId)
    {
        return Builders<UserRatingDocument>.Filter.Eq(document => document.TargetType, targetType)
            & Builders<UserRatingDocument>.Filter.Eq(document => document.TargetId, targetId.Trim());
    }

    private static FilterDefinition<RatingAggregateDocument> BuildAggregateTargetFilter(RatingTargetType targetType, string targetId)
    {
        return Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, targetType)
            & Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetId, targetId.Trim());
    }

    private static string BuildTargetKey(RatingTargetType targetType, string targetId)
    {
        return $"{targetType}:{targetId}";
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
