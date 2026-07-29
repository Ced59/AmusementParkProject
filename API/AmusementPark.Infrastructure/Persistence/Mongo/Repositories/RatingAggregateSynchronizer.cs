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

        long mutationVersion = await this.ReserveMutationVersionAsync(target, cancellationToken);
        while (true)
        {
            BsonDocument? aggregateValues = await this.userRatingsCollection.Aggregate()
                .Match(BuildUserRatingTargetFilter(target))
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
                target,
                mutationVersion,
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
            FilterDefinition<RatingAggregateDocument> aggregateFilter = BuildAggregateTargetFilter(target);
            RatingAggregateDocument? currentDocument = await this.ratingAggregatesCollection
                .Find(aggregateFilter)
                .FirstOrDefaultAsync(cancellationToken);
            if (currentDocument is null)
            {
                mutationVersion = await this.ReserveMutationVersionAsync(target, cancellationToken);
                continue;
            }

            if (currentDocument.CalculatedVersion >= currentDocument.MutationVersion)
            {
                return ToVisibleAggregate(currentDocument);
            }

            mutationVersion = currentDocument.MutationVersion;
        }
    }

    private async Task<long> ReserveMutationVersionAsync(
        RatingAggregateTarget target,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = DateTime.UtcNow;
        FilterDefinition<RatingAggregateDocument> aggregateFilter = BuildAggregateTargetFilter(target);
        UpdateDefinition<RatingAggregateDocument> reserveUpdate = Builders<RatingAggregateDocument>.Update
            .SetOnInsert(document => document.Id, Guid.NewGuid().ToString("N"))
            .SetOnInsert(document => document.CreatedAt, nowUtc)
            .SetOnInsert(document => document.UpdatedAt, nowUtc)
            .SetOnInsert(document => document.CalculatedVersion, 0)
            .SetOnInsert(document => document.TargetType, target.TargetType)
            .SetOnInsert(document => document.TargetId, target.TargetId)
            .SetOnInsert(document => document.ParkId, target.ParkId)
            .SetOnInsert(document => document.ParkItemCategory, target.ParkItemCategory)
            .SetOnInsert(document => document.ParkItemType, target.ParkItemType)
            .SetOnInsert(document => document.RatingCount, 0)
            .SetOnInsert(document => document.RatingSum, 0d)
            .SetOnInsert(document => document.AverageRating, 0d)
            .SetOnInsert(document => document.BayesianScore, RatingScoreCalculator.PriorMean)
            .Inc(document => document.MutationVersion, 1);
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
            document = await this.IncrementExistingMutationVersionAsync(aggregateFilter, cancellationToken);
        }
        catch (MongoCommandException exception) when (exception.Code == 11000)
        {
            document = await this.IncrementExistingMutationVersionAsync(aggregateFilter, cancellationToken);
        }

        if (document is null)
        {
            throw new InvalidOperationException("Unable to reserve the rating aggregate mutation version.");
        }

        return document.MutationVersion;
    }

    private async Task<RatingAggregateDocument?> IncrementExistingMutationVersionAsync(
        FilterDefinition<RatingAggregateDocument> aggregateFilter,
        CancellationToken cancellationToken)
    {
        UpdateDefinition<RatingAggregateDocument> increment = Builders<RatingAggregateDocument>.Update
            .Inc(document => document.MutationVersion, 1);
        FindOneAndUpdateOptions<RatingAggregateDocument> options = new FindOneAndUpdateOptions<RatingAggregateDocument>
        {
            IsUpsert = false,
            ReturnDocument = ReturnDocument.After,
        };
        return await this.ratingAggregatesCollection.FindOneAndUpdateAsync(
            aggregateFilter,
            increment,
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
        UpdateDefinition<RatingAggregateDocument> update = Builders<RatingAggregateDocument>.Update
            .Set(document => document.CalculatedVersion, mutationVersion)
            .Set(document => document.TargetType, target.TargetType)
            .Set(document => document.TargetId, target.TargetId)
            .Set(document => document.ParkId, target.ParkId)
            .Set(document => document.ParkItemCategory, target.ParkItemCategory)
            .Set(document => document.ParkItemType, target.ParkItemType)
            .Set(document => document.RatingCount, ratingCount)
            .Set(document => document.RatingSum, ratingSum)
            .Set(document => document.AverageRating, averageRating)
            .Set(document => document.BayesianScore, bayesianScore)
            .Set(document => document.UpdatedAt, nowUtc);
        update = lastRatedAtUtc.HasValue
            ? Builders<RatingAggregateDocument>.Update.Combine(
                update,
                Builders<RatingAggregateDocument>.Update.Set(document => document.LastRatedAtUtc, lastRatedAtUtc.Value))
            : Builders<RatingAggregateDocument>.Update.Combine(
                update,
                Builders<RatingAggregateDocument>.Update.Unset(document => document.LastRatedAtUtc));
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
