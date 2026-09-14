using AmusementPark.Core.Domain.Parks;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

/// <summary>
/// Marque explicitement les conditions historiques comme non sourcées sans inventer de preuve.
/// </summary>
internal sealed class AttractionAccessConditionProvenanceMigration
{
    private readonly IMongoCollection<BsonDocument> collection;

    public AttractionAccessConditionProvenanceMigration(IMongoCollection<BsonDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<long> MigrateAsync(CancellationToken cancellationToken)
    {
        UpdateResult result = await this.collection.UpdateManyAsync(
            BuildFilter(),
            new PipelineUpdateDefinition<BsonDocument>(BuildPipeline()),
            cancellationToken: cancellationToken);

        return result.ModifiedCount;
    }

    internal static BsonDocument BuildFilter()
    {
        return new BsonDocument(
            "attractionDetails.accessConditions",
            new BsonDocument(
                "$elemMatch",
                new BsonDocument(
                    "$or",
                    new BsonArray
                    {
                        new BsonDocument(
                            "provenanceSchemaVersion",
                            new BsonDocument("$exists", false)),
                        new BsonDocument(
                            "provenanceSchemaVersion",
                            BsonNull.Value),
                        new BsonDocument(
                            "provenanceSchemaVersion",
                            new BsonDocument(
                                "$lt",
                                AttractionAccessCondition.CurrentProvenanceSchemaVersion)),
                    })));
    }

    internal static BsonDocument[] BuildPipeline()
    {
        BsonDocument outdatedCondition = new BsonDocument(
            "$lt",
            new BsonArray
            {
                new BsonDocument(
                    "$ifNull",
                    new BsonArray { "$$condition.provenanceSchemaVersion", 0 }),
                AttractionAccessCondition.CurrentProvenanceSchemaVersion,
            });
        BsonDocument migratedCondition = new BsonDocument(
            "$mergeObjects",
            new BsonArray
            {
                BuildLegacyDefaults(),
                "$$condition",
                new BsonDocument(
                    "provenanceSchemaVersion",
                    AttractionAccessCondition.CurrentProvenanceSchemaVersion),
            });
        BsonDocument mapExpression = new BsonDocument
        {
            {
                "input",
                new BsonDocument(
                    "$ifNull",
                    new BsonArray
                    {
                        "$attractionDetails.accessConditions",
                        new BsonArray(),
                    })
            },
            { "as", "condition" },
            {
                "in",
                new BsonDocument(
                    "$cond",
                    new BsonArray
                    {
                        outdatedCondition,
                        migratedCondition,
                        "$$condition",
                    })
            },
        };

        return new[]
        {
            new BsonDocument(
                "$set",
                new BsonDocument(
                    "attractionDetails.accessConditions",
                    new BsonDocument("$map", mapExpression))),
        };
    }

    internal static BsonDocument BuildLegacyDefaults()
    {
        return new BsonDocument
        {
            {
                "provenanceSchemaVersion",
                AttractionAccessCondition.CurrentProvenanceSchemaVersion
            },
            { "sourceKind", AttractionAccessConditionSourceKind.Unknown.ToString() },
            { "sourceSummary", new BsonArray() },
            { "sourceConfidence", AttractionAccessConditionConfidence.Unknown.ToString() },
            { "scope", AttractionAccessConditionScope.Attraction.ToString() },
        };
    }
}
