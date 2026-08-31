using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class RatingAggregateSourceReader
{
    private static readonly TimeSpan QueryMaxTime = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<UserRatingDocument> userRatingsCollection;

    public RatingAggregateSourceReader(IMongoCollection<UserRatingDocument> userRatingsCollection)
    {
        this.userRatingsCollection = userRatingsCollection;
    }

    public async Task<IReadOnlyCollection<RatingAggregateSourceFact>> ReadAsync(
        IReadOnlyCollection<RatingAggregateSourceTarget> targets,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<BsonDocument> pipeline = BuildPipeline(targets);
        if (pipeline.Count == 0)
        {
            return Array.Empty<RatingAggregateSourceFact>();
        }

        PipelineDefinition<UserRatingDocument, BsonDocument> pipelineDefinition = pipeline.ToArray();
        List<BsonDocument> documents = await this.userRatingsCollection
            .Aggregate<BsonDocument>(pipelineDefinition, new AggregateOptions
            {
                AllowDiskUse = false,
                MaxTime = QueryMaxTime,
            })
            .ToListAsync(cancellationToken);

        return documents
            .Select(TryMap)
            .Where(static fact => fact is not null)
            .Select(static fact => fact!)
            .ToList();
    }

    internal static IReadOnlyCollection<BsonDocument> BuildPipeline(
        IReadOnlyCollection<RatingAggregateSourceTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        List<RatingAggregateSourceTarget> normalizedTargets = targets
            .Where(static target => target is not null
                && Enum.IsDefined(target.TargetType)
                && !string.IsNullOrWhiteSpace(target.TargetId))
            .Select(static target => new RatingAggregateSourceTarget(
                target.TargetType,
                target.TargetId.Trim()))
            .Distinct()
            .ToList();
        if (normalizedTargets.Count == 0)
        {
            return Array.Empty<BsonDocument>();
        }

        List<BsonDocument> targetFilters = normalizedTargets
            .GroupBy(static target => target.TargetType)
            .Select(group => new BsonDocument
            {
                { "targetType", group.Key.ToString() },
                {
                    "targetId",
                    new BsonDocument("$in", new BsonArray(
                        group.Select(static target => (BsonValue)target.TargetId)))
                },
            })
            .ToList();
        BsonDocument targetMatch = new BsonDocument(
            "$match",
            new BsonDocument("$or", new BsonArray(targetFilters)));
        BsonDocument perContributorGroup = new BsonDocument("$group", new BsonDocument
        {
            {
                "_id",
                new BsonDocument
                {
                    { "targetType", "$targetType" },
                    { "targetId", "$targetId" },
                    { "userId", RatingAggregateSynchronizer.BuildCanonicalUserIdExpression() },
                }
            },
            { "observationCount", new BsonDocument("$sum", 1) },
            { "ratingSum", new BsonDocument("$sum", "$value") },
        });
        BsonDocument perTargetGroup = new BsonDocument("$group", new BsonDocument
        {
            {
                "_id",
                new BsonDocument
                {
                    { "targetType", "$_id.targetType" },
                    { "targetId", "$_id.targetId" },
                }
            },
            { "uniqueContributorCount", new BsonDocument("$sum", 1) },
            { "ratingObservationCount", new BsonDocument("$sum", "$observationCount") },
            { "ratingSum", new BsonDocument("$sum", "$ratingSum") },
        });
        BsonDocument project = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 },
            { "targetType", "$_id.targetType" },
            { "targetId", "$_id.targetId" },
            { "uniqueContributorCount", 1 },
            { "ratingObservationCount", 1 },
            { "ratingSum", 1 },
        });

        return new[]
        {
            targetMatch,
            RatingAggregateSynchronizer.BuildValidRatingSourceMatchStage(),
            perContributorGroup,
            perTargetGroup,
            project,
            new BsonDocument("$sort", new BsonDocument
            {
                { "targetType", 1 },
                { "targetId", 1 },
            }),
        };
    }

    internal static bool TryVerifyAndHydrateProjection(
        RatingAggregate aggregate,
        RatingAggregateSourceFact? source)
    {
        ArgumentNullException.ThrowIfNull(aggregate);

        long sourceRatingObservationCount = source?.RatingObservationCount ?? 0;
        long sourceUniqueContributorCount = source?.UniqueContributorCount ?? 0;
        double sourceRatingSum = source?.RatingSum ?? 0d;
        bool sourceIdentityMatches = source is null
            || (aggregate.TargetType == source.TargetType
                && string.Equals(aggregate.TargetId, source.TargetId, StringComparison.Ordinal));
        long verifiedUniqueContributorCount = 0;
        bool sourceProjectionIsValid = sourceIdentityMatches
            && RatingAggregate.TryResolveVerifiedSourceProjection(
                aggregate.RatingCount,
                aggregate.UniqueContributorCount,
                aggregate.RatingSum,
                aggregate.AverageRating,
                aggregate.BayesianScore,
                sourceRatingObservationCount,
                sourceUniqueContributorCount,
                sourceRatingSum,
                out verifiedUniqueContributorCount);
        if (sourceProjectionIsValid)
        {
            aggregate.UniqueContributorCount = verifiedUniqueContributorCount;
        }

        return sourceProjectionIsValid;
    }

    private static RatingAggregateSourceFact? TryMap(BsonDocument document)
    {
        if (!document.TryGetValue("targetType", out BsonValue? targetTypeValue)
            || !targetTypeValue.IsString
            || !Enum.TryParse(targetTypeValue.AsString, true, out RatingTargetType targetType)
            || !Enum.IsDefined(targetType)
            || !document.TryGetValue("targetId", out BsonValue? targetIdValue)
            || !targetIdValue.IsString
            || string.IsNullOrWhiteSpace(targetIdValue.AsString)
            || !TryReadNonNegativeInt64(document, "uniqueContributorCount", out long uniqueContributorCount)
            || !TryReadNonNegativeInt64(document, "ratingObservationCount", out long ratingObservationCount)
            || !document.TryGetValue("ratingSum", out BsonValue? ratingSumValue)
            || !ratingSumValue.IsNumeric)
        {
            return null;
        }

        double ratingSum = ratingSumValue.ToDouble();
        if (!double.IsFinite(ratingSum))
        {
            return null;
        }

        return new RatingAggregateSourceFact(
            targetType,
            targetIdValue.AsString.Trim(),
            uniqueContributorCount,
            ratingObservationCount,
            ratingSum);
    }

    private static bool TryReadNonNegativeInt64(BsonDocument document, string name, out long value)
    {
        if (!document.TryGetValue(name, out BsonValue? bsonValue)
            || !bsonValue.IsNumeric)
        {
            value = 0;
            return false;
        }

        value = bsonValue.ToInt64();
        return value >= 0;
    }
}
