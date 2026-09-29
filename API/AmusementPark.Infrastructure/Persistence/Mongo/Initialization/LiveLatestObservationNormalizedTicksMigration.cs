using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

internal sealed class LiveLatestObservationNormalizedTicksMigration
{
    private const long UnixEpochTicks = 621355968000000000L;
    private readonly IMongoCollection<BsonDocument> collection;

    public LiveLatestObservationNormalizedTicksMigration(IMongoCollection<BsonDocument> collection)
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
            "provenance.normalizedAtUtcTicks",
            new BsonDocument("$exists", false));
    }

    internal static BsonDocument[] BuildPipeline()
    {
        BsonDocument normalizedDateTicks = new BsonDocument(
            "$add",
            new BsonArray
            {
                new BsonDocument(
                    "$multiply",
                    new BsonArray
                    {
                        new BsonDocument("$toLong", "$provenance.normalizedAtUtc"),
                        TimeSpan.TicksPerMillisecond,
                    }),
                UnixEpochTicks,
            });
        BsonDocument receivedTicks = new BsonDocument(
            "$ifNull",
            new BsonArray
            {
                "$provenance.receivedAtUtcTicks",
                normalizedDateTicks,
            });
        BsonDocument exactTicks = new BsonDocument(
            "$max",
            new BsonArray { normalizedDateTicks, receivedTicks });
        return new[]
        {
            new BsonDocument(
                "$set",
                new BsonDocument("provenance.normalizedAtUtcTicks", exactTicks)),
        };
    }
}
