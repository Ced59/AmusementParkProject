using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Ratings;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class RatingEvidenceReader : IRatingEvidenceReader
{
    private static readonly TimeSpan QueryMaxTime = TimeSpan.FromSeconds(10);

    private readonly IMongoCollection<UserRatingDocument> userRatingsCollection;
    private readonly IMongoCollection<ParkItemDocument> parkItemsCollection;

    public RatingEvidenceReader(IMongoDatabase database, MongoDbSettings settings)
    {
        this.userRatingsCollection = database.GetCollection<UserRatingDocument>(settings.UserRatingsCollectionName);
        this.parkItemsCollection = database.GetCollection<ParkItemDocument>(settings.ParkItemsCollectionName);
    }

    public async Task<ParkRankingEvidenceFactsBatch> ReadParkRankingFactsAsync(
        IReadOnlyCollection<RatingEvidenceTarget> targets,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(targets);

        List<RatingEvidenceTarget> normalizedTargets = targets
            .Where(static target => target is not null
                && !string.IsNullOrWhiteSpace(target.TargetId)
                && !string.IsNullOrWhiteSpace(target.ParkId)
                && Enum.IsDefined(target.TargetType))
            .Select(static target => new RatingEvidenceTarget(
                target.TargetType,
                target.TargetId.Trim(),
                target.ParkId.Trim()))
            .Distinct()
            .ToList();
        if (normalizedTargets.Count == 0)
        {
            return ParkRankingEvidenceFactsBatch.Empty;
        }

        IReadOnlyCollection<BsonDocument> pipeline = BuildContributorPipeline(normalizedTargets);
        PipelineDefinition<UserRatingDocument, BsonDocument> pipelineDefinition = pipeline.ToArray();
        Task<List<BsonDocument>> contributorDocumentsTask = this.userRatingsCollection
            .Aggregate<BsonDocument>(pipelineDefinition, new AggregateOptions
            {
                AllowDiskUse = false,
                MaxTime = QueryMaxTime,
            })
            .ToListAsync(cancellationToken);
        Task<List<PublicParkItemProjection>> publicItemDocumentsTask = this.LoadPublicItemDocumentsAsync(
            normalizedTargets.Select(static target => target.ParkId).Distinct(StringComparer.Ordinal).ToList(),
            cancellationToken);

        await Task.WhenAll(contributorDocumentsTask, publicItemDocumentsTask);

        List<BsonDocument> contributorDocuments = await contributorDocumentsTask;
        List<PublicParkItemProjection> publicItemDocuments = await publicItemDocumentsTask;
        List<ParkRankingContributorFacts> contributors = contributorDocuments
            .Select(TryMapContributorFacts)
            .Where(static facts => facts is not null)
            .Select(static facts => facts!)
            .ToList();
        IReadOnlyCollection<PublicParkItemEvidenceFact> publicItems = BuildPublicItemFacts(
            publicItemDocuments);

        return new ParkRankingEvidenceFactsBatch(contributors, publicItems);
    }

    internal static IReadOnlyCollection<BsonDocument> BuildContributorPipeline(
        IReadOnlyCollection<RatingEvidenceTarget> targets)
    {
        ArgumentNullException.ThrowIfNull(targets);

        List<string> parkTargetIds = targets
            .Where(static target => target.TargetType == RatingTargetType.Park)
            .Select(static target => target.TargetId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        List<string> parkItemTargetIds = targets
            .Where(static target => target.TargetType == RatingTargetType.ParkItem)
            .Select(static target => target.TargetId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        List<string> parkIds = targets
            .Select(static target => target.ParkId)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        List<BsonDocument> targetFilters = new List<BsonDocument>();
        if (parkTargetIds.Count > 0)
        {
            targetFilters.Add(BuildTargetFilter(RatingTargetType.Park, parkTargetIds));
        }

        if (parkItemTargetIds.Count > 0)
        {
            targetFilters.Add(BuildTargetFilter(RatingTargetType.ParkItem, parkItemTargetIds));
        }

        if (targetFilters.Count == 0 || parkIds.Count == 0)
        {
            return Array.Empty<BsonDocument>();
        }

        BsonDocument match = new BsonDocument("$match", new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$or", new BsonArray(targetFilters)),
            new BsonDocument("parkId", new BsonDocument("$in", ToBsonArray(parkIds))),
            new BsonDocument("userId", new BsonDocument
            {
                { "$type", "string" },
                { "$ne", string.Empty },
            }),
        }));
        BsonDocument perUserGroup = new BsonDocument("$group", new BsonDocument
        {
            {
                "_id",
                new BsonDocument
                {
                    { "parkId", "$parkId" },
                    { "userId", "$userId" },
                }
            },
            { "ratingObservationCount", new BsonDocument("$sum", 1) },
            {
                "directRatingCount",
                new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { "$targetType", RatingTargetType.Park.ToString() }),
                    1,
                    0,
                }))
            },
            {
                "itemRatingCount",
                new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$eq", new BsonArray { "$targetType", RatingTargetType.ParkItem.ToString() }),
                    1,
                    0,
                }))
            },
        });
        BsonDocument perParkGroup = new BsonDocument("$group", new BsonDocument
        {
            { "_id", "$_id.parkId" },
            { "uniqueContributorCount", new BsonDocument("$sum", 1) },
            { "ratingObservationCount", new BsonDocument("$sum", "$ratingObservationCount") },
            {
                "directParkContributorCount",
                new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$gt", new BsonArray { "$directRatingCount", 0 }),
                    1,
                    0,
                }))
            },
            {
                "itemContributorCount",
                new BsonDocument("$sum", new BsonDocument("$cond", new BsonArray
                {
                    new BsonDocument("$gt", new BsonArray { "$itemRatingCount", 0 }),
                    1,
                    0,
                }))
            },
        });
        BsonDocument project = new BsonDocument("$project", new BsonDocument
        {
            { "_id", 0 },
            { "parkId", "$_id" },
            { "uniqueContributorCount", 1 },
            { "ratingObservationCount", 1 },
            { "directParkContributorCount", 1 },
            { "itemContributorCount", 1 },
        });

        return new[]
        {
            match,
            new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$gt", new BsonArray
            {
                new BsonDocument("$strLenCP", new BsonDocument("$trim", new BsonDocument("input", "$userId"))),
                0,
            }))),
            new BsonDocument("$match", new BsonDocument(
                "$expr",
                RatingValueMongoExpressions.BuildIsExactValidRatingValue("$value"))),
            perUserGroup,
            perParkGroup,
            project,
            new BsonDocument("$sort", new BsonDocument("parkId", 1)),
        };
    }

    internal static IReadOnlyCollection<PublicParkItemEvidenceFact> BuildPublicItemFacts(
        IReadOnlyCollection<PublicParkItemProjection> documents)
    {
        ArgumentNullException.ThrowIfNull(documents);

        return documents
            .Where(static document => !string.IsNullOrWhiteSpace(document.Id)
                && !string.IsNullOrWhiteSpace(document.ParkId)
                && ParkItemStatusNormalizer.CanAppearInCurrentRatingRankings(
                    document.Category,
                    document.AttractionStatus))
            .Select(static document => new PublicParkItemEvidenceFact(
                document.ParkId.Trim(),
                document.Id.Trim(),
                document.Category))
            .Distinct()
            .ToList();
    }

    private async Task<List<PublicParkItemProjection>> LoadPublicItemDocumentsAsync(
        IReadOnlyCollection<string> parkIds,
        CancellationToken cancellationToken)
    {
        FilterDefinition<ParkItemDocument> filter = Builders<ParkItemDocument>.Filter.In(
                document => document.ParkId,
                parkIds)
            & Builders<ParkItemDocument>.Filter.Eq(document => document.IsVisible, true);
        FindOptions findOptions = new FindOptions
        {
            MaxTime = QueryMaxTime,
        };

        return await this.parkItemsCollection.Find(filter, findOptions)
            .Project(static document => new PublicParkItemProjection
            {
                Id = document.Id,
                ParkId = document.ParkId,
                Category = document.Category,
                AttractionStatus = document.AttractionDetails!.Status,
            })
            .ToListAsync(cancellationToken);
    }

    private static BsonDocument BuildTargetFilter(RatingTargetType targetType, IReadOnlyCollection<string> targetIds)
    {
        return new BsonDocument
        {
            { "targetType", targetType.ToString() },
            { "targetId", new BsonDocument("$in", ToBsonArray(targetIds)) },
        };
    }

    private static BsonArray ToBsonArray(IEnumerable<string> values)
    {
        return new BsonArray(values.Select(static value => (BsonValue)value));
    }

    private static ParkRankingContributorFacts? TryMapContributorFacts(BsonDocument document)
    {
        if (!document.TryGetValue("parkId", out BsonValue? parkIdValue)
            || !parkIdValue.IsString
            || string.IsNullOrWhiteSpace(parkIdValue.AsString)
            || !TryReadNonNegativeInt64(document, "uniqueContributorCount", out long uniqueContributorCount)
            || !TryReadNonNegativeInt64(document, "ratingObservationCount", out long ratingObservationCount)
            || !TryReadNonNegativeInt64(document, "directParkContributorCount", out long directParkContributorCount)
            || !TryReadNonNegativeInt64(document, "itemContributorCount", out long itemContributorCount))
        {
            return null;
        }

        return new ParkRankingContributorFacts(
            parkIdValue.AsString.Trim(),
            uniqueContributorCount,
            ratingObservationCount,
            directParkContributorCount,
            itemContributorCount);
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

    internal sealed class PublicParkItemProjection
    {
        public string Id { get; init; } = string.Empty;

        public string ParkId { get; init; } = string.Empty;

        public ParkItemCategory Category { get; init; }

        public string? AttractionStatus { get; init; }
    }
}
