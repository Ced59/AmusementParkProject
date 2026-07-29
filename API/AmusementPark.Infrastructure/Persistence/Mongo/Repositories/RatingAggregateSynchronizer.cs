using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Maintient la projection d'agrégat cohérente sur un MongoDB autonome grâce à
/// des versions monotones et des écritures conditionnelles.
/// </summary>
internal sealed class RatingAggregateSynchronizer
{
    private readonly IMongoCollection<UserRatingDocument> userRatingsCollection;
    private readonly IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection;

    public RatingAggregateSynchronizer(
        IMongoCollection<UserRatingDocument> userRatingsCollection,
        IMongoCollection<RatingAggregateDocument> ratingAggregatesCollection)
    {
        this.userRatingsCollection = userRatingsCollection;
        this.ratingAggregatesCollection = ratingAggregatesCollection;
    }

    public async Task<RatingAggregate?> RecalculateAsync(
        RatingAggregateTarget target,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(target);

        RatingAggregatePendingMutation pendingMutation =
            await this.ReserveMutationVersionAsync(target, cancellationToken);
        while (true)
        {
            BsonDocument? aggregateValues = await this.userRatingsCollection.Aggregate()
                .Match(BuildUserRatingTargetFilter(pendingMutation.Target))
                .Group(new BsonDocument
                {
                    { "_id", BsonNull.Value },
                    { "count", new BsonDocument("$sum", 1) },
                    { "sum", new BsonDocument("$sum", "$value") },
                    { "lastRatedAtUtc", new BsonDocument("$max", "$updatedAt") },
                })
                .FirstOrDefaultAsync(cancellationToken);

            long ratingCount = aggregateValues?.GetValue("count", BsonValue.Create(0)).ToInt64() ?? 0;
            double ratingSum = aggregateValues?.GetValue("sum", BsonValue.Create(0d)).ToDouble() ?? 0d;
            double averageRating = RatingScoreCalculator.CalculateAverage(ratingSum, ratingCount);
            double bayesianScore = ratingCount > 0
                ? RatingScoreCalculator.CalculateBayesianScore(ratingSum, ratingCount)
                : RatingScoreCalculator.PriorMean;
            DateTime? lastRatedAtUtc = aggregateValues is null
                ? null
                : ReadOptionalDateTime(aggregateValues, "lastRatedAtUtc");

            RatingAggregateDocument? committedDocument = await this.TryCommitSnapshotAsync(
                pendingMutation.Target,
                pendingMutation.Version,
                ratingCount,
                ratingSum,
                averageRating,
                bayesianScore,
                lastRatedAtUtc,
                cancellationToken);
            if (committedDocument is not null)
            {
                return ToVisibleAggregate(committedDocument);
            }

            // Une mutation plus récente a invalidé ce snapshot. Le recalcul courant
            // retourne sa projection terminée ou aide à finaliser la dernière version.
            FilterDefinition<RatingAggregateDocument> aggregateFilter =
                BuildAggregateTargetFilter(pendingMutation.Target);
            RatingAggregateDocument? currentDocument = await this.ratingAggregatesCollection
                .Find(aggregateFilter)
                .FirstOrDefaultAsync(cancellationToken);
            if (currentDocument is null)
            {
                pendingMutation = await this.ReserveMutationVersionAsync(target, cancellationToken);
                continue;
            }

            if (currentDocument.CalculatedVersion >= currentDocument.MutationVersion)
            {
                return ToVisibleAggregate(currentDocument);
            }

            pendingMutation = ToPendingMutation(currentDocument);
        }
    }

    private async Task<RatingAggregatePendingMutation> ReserveMutationVersionAsync(
        RatingAggregateTarget target,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = DateTime.UtcNow;
        FilterDefinition<RatingAggregateDocument> aggregateFilter = BuildAggregateTargetFilter(target);
        UpdateDefinition<RatingAggregateDocument> reserveUpdate = BuildReserveUpdate(target, nowUtc);
        FindOneAndUpdateOptions<RatingAggregateDocument> options = new FindOneAndUpdateOptions<RatingAggregateDocument>
        {
            IsUpsert = true,
            ReturnDocument = ReturnDocument.After,
        };

        RatingAggregateDocument? document;
        try
        {
            document = await this.ratingAggregatesCollection.FindOneAndUpdateAsync(
                aggregateFilter,
                reserveUpdate,
                options,
                cancellationToken);
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            document = await this.ReserveExistingMutationVersionAsync(
                aggregateFilter,
                target,
                nowUtc,
                cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            document = await this.ReserveExistingMutationVersionAsync(
                aggregateFilter,
                target,
                nowUtc,
                cancellationToken);
        }

        if (document is null)
        {
            throw new InvalidOperationException("Unable to reserve the rating aggregate mutation version.");
        }

        return ToPendingMutation(document);
    }

    internal static UpdateDefinition<RatingAggregateDocument> BuildReserveUpdate(
        RatingAggregateTarget target,
        DateTime nowUtc)
    {
        return Builders<RatingAggregateDocument>.Update
            .SetOnInsert(document => document.Id, Guid.NewGuid().ToString("N"))
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .SetOnInsert(document => document.UpdatedAt, nowUtc)
            .SetOnInsert(document => document.CalculatedVersion, 0)
            .SetOnInsert(document => document.TargetType, target.TargetType)
            .SetOnInsert(document => document.TargetId, target.TargetId.Trim())
            .SetOnInsert(document => document.RatingCount, 0)
            .SetOnInsert(document => document.RatingSum, 0d)
            .SetOnInsert(document => document.AverageRating, 0d)
            .SetOnInsert(document => document.BayesianScore, RatingScoreCalculator.PriorMean)
            .Set(document => document.PendingParkId, target.ParkId.Trim())
            .Set(document => document.PendingParkItemCategory, target.ParkItemCategory)
            .Set(document => document.PendingParkItemType, target.ParkItemType)
            .Inc(document => document.MutationVersion, 1);
    }

    private async Task<RatingAggregateDocument?> ReserveExistingMutationVersionAsync(
        FilterDefinition<RatingAggregateDocument> aggregateFilter,
        RatingAggregateTarget target,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<RatingAggregateDocument> reserveUpdate = BuildReserveUpdate(target, nowUtc);
        FindOneAndUpdateOptions<RatingAggregateDocument> options = new FindOneAndUpdateOptions<RatingAggregateDocument>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After,
        };
        return await this.ratingAggregatesCollection.FindOneAndUpdateAsync(
            aggregateFilter,
            reserveUpdate,
            options,
            cancellationToken);
    }

    private async Task<RatingAggregateDocument?> TryCommitSnapshotAsync(
        RatingAggregateTarget target,
        long mutationVersion,
        long ratingCount,
        double ratingSum,
        double averageRating,
        double bayesianScore,
        DateTime? lastRatedAtUtc,
        CancellationToken cancellationToken)
    {
        FilterDefinition<RatingAggregateDocument> commitFilter =
            BuildCommitFilter(target.TargetType, target.TargetId, mutationVersion);
        DateTime nowUtc = DateTime.UtcNow;
        UpdateDefinition<RatingAggregateDocument> update = BuildCommitUpdate(
            target,
            mutationVersion,
            ratingCount,
            ratingSum,
            averageRating,
            bayesianScore,
            lastRatedAtUtc,
            nowUtc);
        FindOneAndUpdateOptions<RatingAggregateDocument> options = new FindOneAndUpdateOptions<RatingAggregateDocument>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After,
        };

        return await this.ratingAggregatesCollection.FindOneAndUpdateAsync(
            commitFilter,
            update,
            options,
            cancellationToken);
    }

    internal static UpdateDefinition<RatingAggregateDocument> BuildCommitUpdate(
        RatingAggregateTarget target,
        long mutationVersion,
        long ratingCount,
        double ratingSum,
        double averageRating,
        double bayesianScore,
        DateTime? lastRatedAtUtc,
        DateTime nowUtc)
    {
        UpdateDefinition<RatingAggregateDocument> update = Builders<RatingAggregateDocument>.Update
            .Set(document => document.CalculatedVersion, mutationVersion)
            .Set(document => document.TargetType, target.TargetType)
            .Set(document => document.TargetId, target.TargetId.Trim())
            .Set(document => document.ParkId, target.ParkId.Trim())
            .Set(document => document.ParkItemCategory, target.ParkItemCategory)
            .Set(document => document.ParkItemType, target.ParkItemType)
            .Set(document => document.RatingCount, ratingCount)
            .Set(document => document.RatingSum, ratingSum)
            .Set(document => document.AverageRating, averageRating)
            .Set(document => document.BayesianScore, bayesianScore)
            .Set(document => document.UpdatedAt, nowUtc)
            .Unset(document => document.PendingParkId)
            .Unset(document => document.PendingParkItemCategory)
            .Unset(document => document.PendingParkItemType);
        update = lastRatedAtUtc.HasValue
            ? Builders<RatingAggregateDocument>.Update.Combine(
                update,
                Builders<RatingAggregateDocument>.Update.Set(document => document.LastRatedAtUtc, lastRatedAtUtc.Value))
            : Builders<RatingAggregateDocument>.Update.Combine(
                update,
                Builders<RatingAggregateDocument>.Update.Unset(document => document.LastRatedAtUtc));
        return update;
    }

    internal static FilterDefinition<RatingAggregateDocument> BuildCommitFilter(
        RatingTargetType targetType,
        string targetId,
        long mutationVersion)
    {
        FilterDefinition<RatingAggregateDocument> versionFilter =
            Builders<RatingAggregateDocument>.Filter.Eq(document => document.MutationVersion, mutationVersion);
        FilterDefinition<RatingAggregateDocument> pendingCalculationFilter =
            Builders<RatingAggregateDocument>.Filter.Exists(document => document.CalculatedVersion, false)
            | Builders<RatingAggregateDocument>.Filter.Lt(document => document.CalculatedVersion, mutationVersion);
        return BuildAggregateTargetFilter(targetType, targetId)
            & versionFilter
            & pendingCalculationFilter;
    }

    internal static RatingAggregatePendingMutation ToPendingMutation(RatingAggregateDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);

        RatingAggregateTarget target = new RatingAggregateTarget(
            document.TargetType,
            document.TargetId,
            document.PendingParkId
                ?? throw new InvalidOperationException("Pending rating aggregate metadata is missing."),
            document.PendingParkItemCategory,
            document.PendingParkItemType);
        return new RatingAggregatePendingMutation(document.MutationVersion, target);
    }

    private static FilterDefinition<UserRatingDocument> BuildUserRatingTargetFilter(RatingAggregateTarget target)
    {
        return Builders<UserRatingDocument>.Filter.Eq(document => document.TargetType, target.TargetType)
            & Builders<UserRatingDocument>.Filter.Eq(document => document.TargetId, target.TargetId.Trim());
    }

    private static FilterDefinition<RatingAggregateDocument> BuildAggregateTargetFilter(RatingAggregateTarget target)
    {
        return BuildAggregateTargetFilter(target.TargetType, target.TargetId);
    }

    private static FilterDefinition<RatingAggregateDocument> BuildAggregateTargetFilter(
        RatingTargetType targetType,
        string targetId)
    {
        return Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetType, targetType)
            & Builders<RatingAggregateDocument>.Filter.Eq(document => document.TargetId, targetId.Trim());
    }

    private static RatingAggregate? ToVisibleAggregate(RatingAggregateDocument document)
    {
        return document.RatingCount > 0 ? document.ToDomain() : null;
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

internal sealed record RatingAggregatePendingMutation(
    long Version,
    RatingAggregateTarget Target);
