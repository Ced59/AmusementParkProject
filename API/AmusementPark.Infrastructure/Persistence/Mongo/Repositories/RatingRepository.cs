using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Ratings.Models;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RatingRepository : IRatingRepository
{
    private static readonly BsonRegularExpression OperatingStatusExpression =
        BuildOperatingStatusExpression();

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

        UserRatingDocumentMutationResult documentMutation =
            await this.UpsertUserRatingDocumentAsync(rating, cancellationToken);
        bool sourceChanged = documentMutation.SourceChanged;
        RatingAggregate? aggregate;
        if (sourceChanged)
        {
            aggregate = await this.aggregateSynchronizer.RecalculateAsync(aggregateTarget, cancellationToken);
        }
        else
        {
            aggregate = await this.GetAggregateAsync(rating.TargetType, rating.TargetId, cancellationToken);
            if (aggregate is null || aggregate.SourceIntegrityIsValid != true)
            {
                aggregate = await this.aggregateSynchronizer.RecalculateAsync(aggregateTarget, cancellationToken);
                sourceChanged = true;
            }
        }

        return new UserRatingMutationResult(
            sourceChanged,
            documentMutation.Rating,
            aggregate);
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
            RatingAggregate? retainedAggregate = await this.GetAggregateAsync(
                targetType,
                targetId,
                cancellationToken);
            return new UserRatingDeletionResult(false, retainedAggregate);
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
        return new UserRatingDeletionResult(true, aggregate);
    }

    public async Task RepairAggregateAsync(
        RatingTargetType targetType,
        string targetId,
        CancellationToken cancellationToken)
    {
        if (targetType is not RatingTargetType.Park and not RatingTargetType.ParkItem)
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("A rating aggregate target identifier is required.", nameof(targetId));
        }

        string normalizedTargetId = targetId.Trim();
        RatingAggregateTarget? repairTarget;
        if (targetType == RatingTargetType.Park)
        {
            repairTarget = BuildRepairAggregateTarget(
                targetType,
                normalizedTargetId,
                null,
                null,
                null);
        }
        else
        {
            ParkItemDocument? parkItem = await this.parkItemsCollection
                .Find(document => document.Id == normalizedTargetId)
                .FirstOrDefaultAsync(cancellationToken);
            UserRatingDocument? retainedRating = null;
            RatingAggregateDocument? retainedAggregate = null;
            if (parkItem is null)
            {
                FilterDefinition<UserRatingDocument> ratingFilter =
                    Builders<UserRatingDocument>.Filter.Eq(document => document.TargetType, targetType)
                    & Builders<UserRatingDocument>.Filter.Eq(document => document.TargetId, normalizedTargetId);
                retainedRating = await this.userRatingsCollection
                    .Find(ratingFilter)
                    .SortByDescending(document => document.UpdatedAt)
                    .FirstOrDefaultAsync(cancellationToken);
                if (retainedRating is null)
                {
                    retainedAggregate = await this.ratingAggregatesCollection
                        .Find(BuildAggregateTargetFilter(targetType, normalizedTargetId))
                        .FirstOrDefaultAsync(cancellationToken);
                }
            }
            repairTarget = BuildRepairAggregateTarget(
                targetType,
                normalizedTargetId,
                parkItem,
                retainedRating,
                retainedAggregate);
        }
        if (repairTarget is not null)
        {
            await this.aggregateSynchronizer.RecalculateAsync(repairTarget, cancellationToken);
        }
    }

    internal static RatingAggregateTarget? BuildRepairAggregateTarget(
        RatingTargetType targetType,
        string targetId,
        ParkItemDocument? parkItem,
        UserRatingDocument? retainedRating,
        RatingAggregateDocument? retainedAggregate)
    {
        if (string.IsNullOrWhiteSpace(targetId))
        {
            throw new ArgumentException("A rating aggregate target identifier is required.", nameof(targetId));
        }

        string normalizedTargetId = targetId.Trim();
        if (targetType == RatingTargetType.Park)
        {
            return new RatingAggregateTarget(
                targetType,
                normalizedTargetId,
                normalizedTargetId,
                null,
                null);
        }

        if (targetType != RatingTargetType.ParkItem)
        {
            throw new ArgumentOutOfRangeException(nameof(targetType));
        }

        if (parkItem is not null)
        {
            return new RatingAggregateTarget(
                targetType,
                normalizedTargetId,
                NormalizeRepairParkId(parkItem.ParkId),
                parkItem.Category,
                parkItem.Type);
        }

        if (retainedRating is not null)
        {
            return new RatingAggregateTarget(
                targetType,
                normalizedTargetId,
                NormalizeRepairParkId(retainedRating.ParkId),
                retainedRating.ParkItemCategory,
                retainedRating.ParkItemType);
        }

        if (retainedAggregate is null)
        {
            return null;
        }

        return new RatingAggregateTarget(
            targetType,
            normalizedTargetId,
            NormalizeRepairParkId(retainedAggregate.PendingParkId ?? retainedAggregate.ParkId),
            retainedAggregate.PendingParkItemCategory ?? retainedAggregate.ParkItemCategory,
            retainedAggregate.PendingParkItemType ?? retainedAggregate.ParkItemType);
    }

    private static string NormalizeRepairParkId(string? parkId)
    {
        if (string.IsNullOrWhiteSpace(parkId))
        {
            throw new InvalidOperationException("Rating aggregate recovery metadata has no park identifier.");
        }

        return parkId.Trim();
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

    private async Task<UserRatingDocumentMutationResult> UpsertUserRatingDocumentAsync(
        UserRating rating,
        CancellationToken cancellationToken)
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
            ReturnDocument = ReturnDocument.Before,
        };

        UserRatingDocument? previousDocument = await this.userRatingsCollection.FindOneAndUpdateAsync(
            filter,
            update,
            options,
            cancellationToken);
        bool sourceChanged = HasRankingSourceChanged(previousDocument, rating);
        UserRating upsertedRating = new UserRating
        {
            Id = previousDocument?.Id ?? documentId,
            UserId = rating.UserId.Trim(),
            TargetType = rating.TargetType,
            TargetId = rating.TargetId.Trim(),
            ParkId = rating.ParkId.Trim(),
            ParkItemCategory = rating.ParkItemCategory,
            ParkItemType = rating.ParkItemType,
            Value = rating.Value,
            CreatedAtUtc = previousDocument?.CreatedAt ?? nowUtc,
            UpdatedAtUtc = nowUtc,
        };
        return new UserRatingDocumentMutationResult(sourceChanged, upsertedRating);
    }

    internal static bool HasRankingSourceChanged(
        UserRatingDocument? previousDocument,
        UserRating rating)
    {
        ArgumentNullException.ThrowIfNull(rating);
        return previousDocument is null
            || previousDocument.Value != rating.Value
            || !string.Equals(previousDocument.ParkId, rating.ParkId.Trim(), StringComparison.Ordinal)
            || previousDocument.ParkItemCategory != rating.ParkItemCategory
            || previousDocument.ParkItemType != rating.ParkItemType;
    }

    private sealed record UserRatingDocumentMutationResult(
        bool SourceChanged,
        UserRating Rating);

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
        BsonDocument[] parkPipeline = BuildParkRankingCandidatePipeline(
            this.parksCollection.CollectionNamespace.CollectionName,
            RankingCandidateHardLimit + 1);
        List<BsonDocument> parkCandidateBsonDocuments = await this.ratingAggregatesCollection
            .Aggregate<BsonDocument>(parkPipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        List<RatingAggregateDocument> parkDocuments = parkCandidateBsonDocuments
            .Select(static document => BsonSerializer.Deserialize<RatingAggregateDocument>(document))
            .ToList();
        RatingRankingSourceBatch parkItemBatch = await this.LoadVisibleParkItemRankingSourceBatchAsync(
            parkItemCategory,
            effectiveMaxItems,
            null,
            cancellationToken);
        bool isTruncated = IsVisibleRankingSourceSetTruncated(
            parkDocuments.Count,
            parkItemBatch.Sources.Count + (parkItemBatch.IsTruncated ? 1 : 0),
            effectiveMaxItems);
        IReadOnlyCollection<RatingRankingItemResult> parkSources =
            await this.EnrichVisibleRankingSourcesAsync(
                parkDocuments.Take(RankingCandidateHardLimit).ToList(),
                cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = parkSources
            .Concat(parkItemBatch.Sources)
            .ToArray();
        return new RatingRankingSourceBatch(sources, isTruncated);
    }

    public async Task<RatingRankingParkCandidateBatch> GetVisibleParkRankingSnapshotCandidateBatchAsync(
        int maxParks,
        CancellationToken cancellationToken)
    {
        int effectiveMaxParks = Math.Clamp(maxParks, 1, RankingCandidateHardLimit);
        BsonDocument[] parkPipeline = BuildParkRankingCandidatePipeline(
            this.parksCollection.CollectionNamespace.CollectionName,
            effectiveMaxParks + 1);
        List<BsonDocument> parkCandidateBsonDocuments = await this.ratingAggregatesCollection
            .Aggregate<BsonDocument>(parkPipeline, cancellationToken: cancellationToken)
            .ToListAsync(cancellationToken);
        List<string> directParkIds = parkCandidateBsonDocuments
            .Select(static document => BsonSerializer.Deserialize<RatingAggregateDocument>(document))
            .Select(static document => document.TargetId)
            .Where(static parkId => !string.IsNullOrWhiteSpace(parkId))
            .ToList();
        BsonDocument[] parkItemPipeline = BuildParkItemRankingParkCandidatePipeline(
            this.ratingAggregatesCollection.CollectionNamespace.CollectionName,
            this.parksCollection.CollectionNamespace.CollectionName,
            effectiveMaxParks + 1);
        List<BsonDocument> parkItemCandidates = await this.parkItemsCollection
            .Aggregate<BsonDocument>(
                parkItemPipeline,
                new AggregateOptions { AllowDiskUse = true },
                cancellationToken)
            .ToListAsync(cancellationToken);
        List<string> combinedParkIds = directParkIds
            .Concat(parkItemCandidates
                .Where(static document => document.TryGetValue("parkId", out BsonValue? value)
                    && value.IsString
                    && !string.IsNullOrWhiteSpace(value.AsString))
                .Select(static document => document["parkId"].AsString))
            .Select(static parkId => parkId.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static parkId => parkId, StringComparer.Ordinal)
            .ToList();
        return new RatingRankingParkCandidateBatch(
            combinedParkIds.Take(effectiveMaxParks).ToArray(),
            combinedParkIds.Count > effectiveMaxParks);
    }

    public async Task<RatingRankingSourceBatch> GetVisibleParkRankingSnapshotSourceBatchAsync(
        IReadOnlyCollection<string> parkIds,
        int maxSourceComponents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(parkIds);
        List<string> normalizedParkIds = NormalizeIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return new RatingRankingSourceBatch(Array.Empty<RatingRankingItemResult>(), false);
        }

        int effectiveMaxSourceComponents = Math.Clamp(
            maxSourceComponents,
            1,
            RatingRankingSnapshotBuildLimits.MaximumSourceComponentCountPerParkBatch);
        FilterDefinition<RatingAggregateDocument> directParkFilter =
            Builders<RatingAggregateDocument>.Filter.Eq(
                document => document.TargetType,
                RatingTargetType.Park)
            & Builders<RatingAggregateDocument>.Filter.In(
                document => document.TargetId,
                normalizedParkIds)
            & Builders<RatingAggregateDocument>.Filter.Gt(
                document => document.RatingCount,
                0);
        List<RatingAggregateDocument> directParkDocuments = await this.ratingAggregatesCollection
            .Find(directParkFilter)
            .Limit(effectiveMaxSourceComponents + 1)
            .ToListAsync(cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> directParkSources =
            await this.EnrichVisibleRankingSourcesAsync(
                directParkDocuments.Take(effectiveMaxSourceComponents).ToArray(),
                cancellationToken);
        if (directParkDocuments.Count > effectiveMaxSourceComponents)
        {
            return new RatingRankingSourceBatch(directParkSources, true);
        }

        int remainingSourceCapacity = effectiveMaxSourceComponents - directParkSources.Count;
        if (remainingSourceCapacity <= 0)
        {
            return new RatingRankingSourceBatch(directParkSources, true);
        }

        RatingRankingSourceBatch parkItemBatch =
            await this.LoadVisibleParkItemRankingSnapshotSourceBatchAsync(
            normalizedParkIds,
            remainingSourceCapacity,
            cancellationToken);
        IReadOnlyCollection<RatingRankingItemResult> sources = directParkSources
            .Concat(parkItemBatch.Sources)
            .ToArray();
        return new RatingRankingSourceBatch(sources, parkItemBatch.IsTruncated);
    }

    private async Task<RatingRankingSourceBatch> LoadVisibleParkItemRankingSnapshotSourceBatchAsync(
        IReadOnlyCollection<string> parkIds,
        int maximumSourceComponents,
        CancellationToken cancellationToken)
    {
        BsonDocument[] pipeline = BuildParkItemRankingSnapshotSourcePipeline(
            this.ratingAggregatesCollection.CollectionNamespace.CollectionName,
            this.parksCollection.CollectionNamespace.CollectionName,
            parkIds,
            maximumSourceComponents + 1);
        List<BsonDocument> bsonDocuments = await this.parkItemsCollection
            .Aggregate<BsonDocument>(
                pipeline,
                new AggregateOptions
                {
                    AllowDiskUse = true,
                    BatchSize = 500,
                },
                cancellationToken)
            .ToListAsync(cancellationToken);
        List<RatingAggregateDocument> documents = bsonDocuments
            .Select(static document => BsonSerializer.Deserialize<RatingAggregateDocument>(document))
            .ToList();
        IReadOnlyCollection<RatingRankingItemResult> sources =
            await this.EnrichVisibleRankingSourcesAsync(
                documents.Take(maximumSourceComponents).ToArray(),
                cancellationToken);
        return new RatingRankingSourceBatch(
            sources,
            documents.Count > maximumSourceComponents);
    }

    public async Task<IReadOnlyCollection<RatingRankingItemResult>> GetVisibleParkItemRankingSourcesAsync(
        ParkItemCategory parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken)
    {
        RatingRankingSourceBatch batch = await this.GetVisibleParkItemRankingSourceBatchAsync(
            parkItemCategory,
            maxItems,
            cancellationToken);
        return batch.Sources;
    }

    public async Task<RatingRankingSourceBatch> GetVisibleParkItemRankingSourceBatchAsync(
        ParkItemCategory parkItemCategory,
        int maxItems,
        CancellationToken cancellationToken)
    {
        int effectiveMaxItems = Math.Clamp(maxItems, 1, RankingCandidateHardLimit);
        return await this.LoadVisibleParkItemRankingSourceBatchAsync(
            parkItemCategory,
            effectiveMaxItems,
            null,
            cancellationToken);
    }

    private async Task<RatingRankingSourceBatch> LoadVisibleParkItemRankingSourceBatchAsync(
        ParkItemCategory? parkItemCategory,
        int effectiveMaxItems,
        IReadOnlyCollection<string>? parkIds,
        CancellationToken cancellationToken)
    {
        const int candidatePageSize = 500;
        List<RatingRankingItemResult> eligibleSources = new List<RatingRankingItemResult>();
        BsonDocument[] pipeline = BuildParkItemRankingCandidatePipeline(
            parkItemCategory,
            this.parkItemsCollection.CollectionNamespace.CollectionName,
            this.parksCollection.CollectionNamespace.CollectionName,
            effectiveMaxItems + 1,
            parkIds);
        AggregateOptions options = new AggregateOptions
        {
            AllowDiskUse = true,
            BatchSize = candidatePageSize,
        };
        using IAsyncCursor<BsonDocument> cursor = await this.ratingAggregatesCollection
            .AggregateAsync<BsonDocument>(pipeline, options, cancellationToken);
        while (eligibleSources.Count <= effectiveMaxItems
               && await cursor.MoveNextAsync(cancellationToken))
        {
            List<RatingAggregateDocument> candidateDocuments = cursor.Current
                .Select(static document => BsonSerializer.Deserialize<RatingAggregateDocument>(document))
                .ToList();
            IReadOnlyCollection<RatingRankingItemResult> enrichedSources =
                await this.EnrichVisibleRankingSourcesAsync(candidateDocuments, cancellationToken);
            eligibleSources.AddRange(enrichedSources.Where(source =>
                !parkItemCategory.HasValue
                || source.ParkItemCategory == parkItemCategory.Value));
        }

        bool isTruncated = IsParkItemRankingSourceSetTruncated(
            eligibleSources.Count,
            effectiveMaxItems);
        IReadOnlyCollection<RatingRankingItemResult> sources = eligibleSources
            .Take(effectiveMaxItems)
            .ToArray();
        return new RatingRankingSourceBatch(sources, isTruncated);
    }

    internal static BsonDocument[] BuildParkItemRankingCandidatePipeline(
        ParkItemCategory? parkItemCategory,
        string parkItemsCollectionName,
        string parksCollectionName,
        int limit,
        IReadOnlyCollection<string>? parkIds = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parkItemsCollectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parksCollectionName);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        BsonDocument parkItemMatch = new BsonDocument
        {
            { "rankingParkItem.isVisible", true },
        };
        if (parkItemCategory.HasValue)
        {
            parkItemMatch.Add("rankingParkItem.category", parkItemCategory.Value.ToString());
        }

        List<string> normalizedParkIds = NormalizeIds(parkIds ?? Array.Empty<string>());
        if (normalizedParkIds.Count > 0)
        {
            parkItemMatch.Add(
                "rankingParkItem.parkId",
                new BsonDocument("$in", new BsonArray(normalizedParkIds)));
        }

        return new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "targetType", RatingTargetType.ParkItem.ToString() },
                { "ratingCount", new BsonDocument("$gt", 0) },
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", parkItemsCollectionName },
                { "localField", "targetId" },
                { "foreignField", "_id" },
                { "as", "rankingParkItem" },
            }),
            new BsonDocument("$unwind", "$rankingParkItem"),
            new BsonDocument("$match", parkItemMatch),
            new BsonDocument("$match", BuildCurrentParkItemRankingEligibilityMatch()),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", parksCollectionName },
                { "localField", "rankingParkItem.parkId" },
                { "foreignField", "_id" },
                { "as", "rankingParentPark" },
            }),
            new BsonDocument("$unwind", "$rankingParentPark"),
            new BsonDocument("$match", new BsonDocument
            {
                { "rankingParentPark.isVisible", true },
                { "rankingParentPark.status", ParkStatus.Operating.ToString() },
            }),
            new BsonDocument("$sort", new BsonDocument
            {
                { "bayesianScore", -1 },
                { "ratingCount", -1 },
                { "averageRating", -1 },
                { "targetType", 1 },
                { "targetId", 1 },
            }),
            new BsonDocument("$limit", limit),
            new BsonDocument("$project", new BsonDocument
            {
                { "rankingParkItem", 0 },
                { "rankingParentPark", 0 },
            }),
        };
    }

    internal static BsonDocument[] BuildParkItemRankingParkCandidatePipeline(
        string ratingAggregatesCollectionName,
        string parksCollectionName,
        int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ratingAggregatesCollectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parksCollectionName);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        return BuildParkItemRankingSnapshotBasePipeline(
                ratingAggregatesCollectionName,
                parksCollectionName,
                null)
            .Concat(new[]
            {
                new BsonDocument("$group", new BsonDocument("_id", "$parkId")),
                new BsonDocument("$sort", new BsonDocument("_id", 1)),
                new BsonDocument("$limit", limit),
                new BsonDocument("$project", new BsonDocument
                {
                    { "_id", 0 },
                    { "parkId", "$_id" },
                }),
            })
            .ToArray();
    }

    internal static BsonDocument[] BuildParkItemRankingSnapshotSourcePipeline(
        string ratingAggregatesCollectionName,
        string parksCollectionName,
        IReadOnlyCollection<string> parkIds,
        int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(ratingAggregatesCollectionName);
        ArgumentException.ThrowIfNullOrWhiteSpace(parksCollectionName);
        ArgumentNullException.ThrowIfNull(parkIds);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);
        List<string> normalizedParkIds = NormalizeIds(parkIds);
        if (normalizedParkIds.Count == 0)
        {
            return Array.Empty<BsonDocument>();
        }

        return BuildParkItemRankingSnapshotBasePipeline(
                ratingAggregatesCollectionName,
                parksCollectionName,
                normalizedParkIds)
            .Concat(new[]
            {
                new BsonDocument("$sort", new BsonDocument
                {
                    { "ratingAggregate.bayesianScore", -1 },
                    { "ratingAggregate.ratingCount", -1 },
                    { "ratingAggregate.averageRating", -1 },
                    { "ratingAggregate.targetId", 1 },
                }),
                new BsonDocument("$limit", limit),
                new BsonDocument("$replaceRoot", new BsonDocument(
                    "newRoot",
                    "$ratingAggregate")),
            })
            .ToArray();
    }

    private static IEnumerable<BsonDocument> BuildParkItemRankingSnapshotBasePipeline(
        string ratingAggregatesCollectionName,
        string parksCollectionName,
        IReadOnlyCollection<string>? parkIds)
    {
        BsonDocument parkItemMatch = new BsonDocument("isVisible", true);
        if (parkIds is not null)
        {
            parkItemMatch.Add("parkId", new BsonDocument("$in", new BsonArray(parkIds)));
        }

        return new[]
        {
            new BsonDocument("$match", parkItemMatch),
            new BsonDocument("$match", BuildCurrentParkItemRankingEligibilityMatch(string.Empty)),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", parksCollectionName },
                { "localField", "parkId" },
                { "foreignField", "_id" },
                { "as", "rankingParentPark" },
            }),
            new BsonDocument("$unwind", "$rankingParentPark"),
            new BsonDocument("$match", new BsonDocument
            {
                { "rankingParentPark.isVisible", true },
                { "rankingParentPark.status", ParkStatus.Operating.ToString() },
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", ratingAggregatesCollectionName },
                { "let", new BsonDocument("rankingTargetId", "$_id") },
                {
                    "pipeline",
                    new BsonArray
                    {
                        new BsonDocument("$match", new BsonDocument
                        {
                            { "targetType", RatingTargetType.ParkItem.ToString() },
                            { "ratingCount", new BsonDocument("$gt", 0) },
                            {
                                "$expr",
                                new BsonDocument("$eq", new BsonArray
                                {
                                    "$targetId",
                                    "$$rankingTargetId",
                                })
                            },
                        }),
                        new BsonDocument("$limit", 1),
                    }
                },
                { "as", "ratingAggregate" },
            }),
            new BsonDocument("$unwind", "$ratingAggregate"),
        };
    }

    private static BsonDocument BuildCurrentParkItemRankingEligibilityMatch(
        string fieldPrefix = "rankingParkItem.")
    {
        string categoryField = $"{fieldPrefix}category";
        string statusField = $"{fieldPrefix}attractionDetails.status";
        BsonArray nonAttractionCategories = new BsonArray(
            Enum.GetValues<ParkItemCategory>()
                .Where(static category => category != ParkItemCategory.Attraction)
                .Select(static category => (BsonValue)category.ToString()));
        return new BsonDocument("$or", new BsonArray
        {
            new BsonDocument
            {
                { categoryField, ParkItemCategory.Attraction.ToString() },
                { statusField, OperatingStatusExpression },
            },
            new BsonDocument
            {
                { categoryField, new BsonDocument("$in", nonAttractionCategories) },
                {
                    statusField,
                    new BsonDocument("$in", new BsonArray
                    {
                        BsonNull.Value,
                        new BsonRegularExpression("^\\s*$"),
                        OperatingStatusExpression,
                    })
                },
            },
        });
    }

    private static BsonRegularExpression BuildOperatingStatusExpression()
    {
        const string ignoredSeparatorPattern = "[ _'-]*";
        IEnumerable<string> aliasPatterns = ParkItemStatusNormalizer.NormalizedOperatingStatusAliases
            .Select(alias => string.Join(
                ignoredSeparatorPattern,
                alias.Select(character => Regex.Escape(character.ToString()))));
        string pattern = $"^\\s*(?:{string.Join("|", aliasPatterns)})\\s*$";
        return new BsonRegularExpression(pattern, "i");
    }

    internal static BsonDocument[] BuildParkRankingCandidatePipeline(
        string parksCollectionName,
        int limit)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(parksCollectionName);
        ArgumentOutOfRangeException.ThrowIfLessThan(limit, 1);

        return new[]
        {
            new BsonDocument("$match", new BsonDocument
            {
                { "targetType", RatingTargetType.Park.ToString() },
                { "ratingCount", new BsonDocument("$gt", 0) },
            }),
            new BsonDocument("$sort", new BsonDocument
            {
                { "bayesianScore", -1 },
                { "ratingCount", -1 },
                { "averageRating", -1 },
                { "targetType", 1 },
                { "targetId", 1 },
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                { "from", parksCollectionName },
                { "localField", "targetId" },
                { "foreignField", "_id" },
                { "as", "rankingPark" },
            }),
            new BsonDocument("$unwind", "$rankingPark"),
            new BsonDocument("$match", new BsonDocument
            {
                { "rankingPark.isVisible", true },
                { "rankingPark.status", ParkStatus.Operating.ToString() },
            }),
            new BsonDocument("$limit", limit),
            new BsonDocument("$project", new BsonDocument
            {
                { "rankingPark", 0 },
            }),
        };
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

    internal static bool IsParkItemRankingSourceSetTruncated(
        int documentCount,
        int documentLimit)
    {
        return documentCount > documentLimit;
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
