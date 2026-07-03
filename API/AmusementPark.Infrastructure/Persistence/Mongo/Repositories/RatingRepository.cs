using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
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

    public RatingRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.userRatingsCollection = database.GetCollection<UserRatingDocument>(settings.UserRatingsCollectionName);
        this.ratingAggregatesCollection = database.GetCollection<RatingAggregateDocument>(settings.RatingAggregatesCollectionName);
        this.parksCollection = database.GetCollection<ParkDocument>(settings.ParksCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<UserRating?> GetUserRatingAsync(string userId, RatingTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        FilterDefinition<UserRatingDocument> filter = BuildUserTargetFilter(userId, targetType, targetId);
        UserRatingDocument? document = await this.userRatingsCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<UserRating> UpsertUserRatingAsync(UserRating rating, CancellationToken cancellationToken)
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

    public async Task<RatingAggregate?> GetAggregateAsync(RatingTargetType targetType, string targetId, CancellationToken cancellationToken)
    {
        FilterDefinition<RatingAggregateDocument> filter = BuildAggregateTargetFilter(targetType, targetId);
        RatingAggregateDocument? document = await this.ratingAggregatesCollection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<RatingAggregate?> RecalculateAggregateAsync(RatingTargetMetadataResult metadata, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(metadata);

        FilterDefinition<UserRatingDocument> ratingFilter = BuildUserRatingTargetFilter(metadata.TargetType, metadata.TargetId);
        BsonDocument? aggregateValues = await this.userRatingsCollection.Aggregate()
            .Match(ratingFilter)
            .Group(new BsonDocument
            {
                { "_id", BsonNull.Value },
                { "count", new BsonDocument("$sum", 1) },
                { "sum", new BsonDocument("$sum", "$value") },
                { "lastRatedAtUtc", new BsonDocument("$max", "$updatedAt") },
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (aggregateValues is null)
        {
            await this.ratingAggregatesCollection.DeleteOneAsync(BuildAggregateTargetFilter(metadata.TargetType, metadata.TargetId), cancellationToken);
            return null;
        }

        long ratingCount = aggregateValues.GetValue("count", BsonValue.Create(0)).ToInt64();
        if (ratingCount <= 0)
        {
            await this.ratingAggregatesCollection.DeleteOneAsync(BuildAggregateTargetFilter(metadata.TargetType, metadata.TargetId), cancellationToken);
            return null;
        }

        double ratingSum = aggregateValues.GetValue("sum", BsonValue.Create(0d)).ToDouble();
        double averageRating = RatingScoreCalculator.CalculateAverage(ratingSum, ratingCount);
        double bayesianScore = RatingScoreCalculator.CalculateBayesianScore(ratingSum, ratingCount);
        DateTime nowUtc = DateTime.UtcNow;
        FilterDefinition<RatingAggregateDocument> aggregateFilter = BuildAggregateTargetFilter(metadata.TargetType, metadata.TargetId);
        UpdateDefinition<RatingAggregateDocument> update = Builders<RatingAggregateDocument>.Update
            .SetOnInsert(document => document.Id, Guid.NewGuid().ToString("N"))
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .Set(document => document.TargetType, metadata.TargetType)
            .Set(document => document.TargetId, metadata.TargetId)
            .Set(document => document.ParkId, metadata.ParkId)
            .Set(document => document.ParkItemCategory, metadata.ParkItemCategory)
            .Set(document => document.ParkItemType, metadata.ParkItemType)
            .Set(document => document.RatingCount, ratingCount)
            .Set(document => document.RatingSum, ratingSum)
            .Set(document => document.AverageRating, averageRating)
            .Set(document => document.BayesianScore, bayesianScore)
            .Set(document => document.LastRatedAtUtc, ReadOptionalDateTime(aggregateValues, "lastRatedAtUtc"))
            .Set(document => document.UpdatedAt, nowUtc);

        FindOneAndUpdateOptions<RatingAggregateDocument> options = new FindOneAndUpdateOptions<RatingAggregateDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        RatingAggregateDocument? document = await this.ratingAggregatesCollection.FindOneAndUpdateAsync(aggregateFilter, update, options, cancellationToken);
        if (document is null)
        {
            document = await this.ratingAggregatesCollection.Find(aggregateFilter).FirstAsync(cancellationToken);
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
        IReadOnlyDictionary<string, string> parkNames = await this.LoadParkNamesAsync(documents.Select(static document => document.ParkId), false, cancellationToken);
        IReadOnlyDictionary<string, ParkItemDocument> parkItems = await this.LoadParkItemsAsync(
            documents.Where(static document => document.TargetType == RatingTargetType.ParkItem).Select(static document => document.TargetId),
            false,
            cancellationToken);
        IReadOnlyDictionary<string, RatingAggregate> aggregates = await this.LoadAggregatesAsync(documents, cancellationToken);

        List<UserRatingListItemResult> items = documents.Select(document =>
        {
            string key = BuildTargetKey(document.TargetType, document.TargetId);
            aggregates.TryGetValue(key, out RatingAggregate? aggregate);
            RatingSummaryResult summary = ToSummary(document.TargetType, document.TargetId, aggregate);
            string? parkName = parkNames.TryGetValue(document.ParkId, out string? resolvedParkName) ? resolvedParkName : null;
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

        if (documents.Count == 0)
        {
            return new UserRatingStatsResult(0, 0d, 0d, 0d, Array.Empty<UserRatingStatBucketResult>(), Array.Empty<UserRatingStatBucketResult>(), Array.Empty<UserRatingStatBucketResult>());
        }

        IReadOnlyDictionary<string, string> parkNames = await this.LoadParkNamesAsync(documents.Select(static document => document.ParkId), false, cancellationToken);
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

    public async Task<IReadOnlyCollection<RatingRankingItemResult>> GetVisibleRankingSourcesAsync(ParkItemCategory? parkItemCategory, int maxItems, CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        List<RatingAggregateDocument> parkDocuments = await this.ratingAggregatesCollection.Find(BuildParkRankingParkFilter())
            .Sort(BuildRankingSort())
            .Limit(RankingCandidateHardLimit)
            .ToListAsync(cancellationToken);
        List<RatingAggregateDocument> parkItemDocuments = await this.ratingAggregatesCollection.Find(BuildParkRankingItemFilter(parkItemCategory))
            .Sort(BuildRankingSort())
            .Limit(effectiveMaxItems)
            .ToListAsync(cancellationToken);
        List<RatingAggregateDocument> candidateDocuments = parkDocuments.Concat(parkItemDocuments).ToList();

        if (candidateDocuments.Count == 0)
        {
            return Array.Empty<RatingRankingItemResult>();
        }

        return await this.EnrichVisibleRankingSourcesAsync(candidateDocuments, cancellationToken);
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
                    document.BayesianScore));
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
                document.BayesianScore));
        }

        return items;
    }

    private async Task<IReadOnlyDictionary<string, RatingAggregate>> LoadAggregatesAsync(IReadOnlyCollection<UserRatingDocument> ratings, CancellationToken cancellationToken)
    {
        List<FilterDefinition<RatingAggregateDocument>> filters = ratings
            .Select(static rating => BuildAggregateTargetFilter(rating.TargetType, rating.TargetId))
            .ToList();

        if (filters.Count == 0)
        {
            return new Dictionary<string, RatingAggregate>(StringComparer.Ordinal);
        }

        List<RatingAggregateDocument> documents = await this.ratingAggregatesCollection.Find(Builders<RatingAggregateDocument>.Filter.Or(filters)).ToListAsync(cancellationToken);
        return documents.ToDictionary(
            static document => BuildTargetKey(document.TargetType, document.TargetId),
            static document => document.ToDomain(),
            StringComparer.Ordinal);
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

    private static RatingSummaryResult ToSummary(RatingTargetType targetType, string targetId, RatingAggregate? aggregate)
    {
        if (aggregate is null)
        {
            return new RatingSummaryResult(targetType, targetId, 0, 0d, RatingScoreCalculator.PriorMean);
        }

        return new RatingSummaryResult(
            aggregate.TargetType,
            aggregate.TargetId,
            aggregate.RatingCount,
            aggregate.AverageRating,
            aggregate.BayesianScore);
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

    private static DateTime? ReadOptionalDateTime(BsonDocument document, string elementName)
    {
        if (!document.TryGetValue(elementName, out BsonValue value) || value.IsBsonNull)
        {
            return null;
        }

        return value is BsonDateTime dateTime ? dateTime.ToUniversalTime() : null;
    }
}
