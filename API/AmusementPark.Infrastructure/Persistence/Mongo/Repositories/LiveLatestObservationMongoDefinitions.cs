using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LiveLatestObservationMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<LiveLatestObservationDocument>> BuildIndexes()
    {
        return new CreateIndexModel<LiveLatestObservationDocument>[]
        {
            new(
                Builders<LiveLatestObservationDocument>.IndexKeys
                    .Ascending(static document => document.SourceId)
                    .Ascending("target.type")
                    .Ascending("target.id"),
                new CreateIndexOptions
                {
                    Name = "idx_live_latest_source_target_unique",
                    Unique = true,
                }),
            new(
                Builders<LiveLatestObservationDocument>.IndexKeys
                    .Ascending("target.parkId")
                    .Ascending("target.type")
                    .Ascending("expiresAtUtc"),
                new CreateIndexOptions { Name = "idx_live_latest_park_type_expiration" }),
        };
    }

    public static FilterDefinition<LiveLatestObservationDocument> BuildNaturalKeyFilter(
        LiveLatestObservationDocument incoming)
    {
        return Builders<LiveLatestObservationDocument>.Filter.And(
            Builders<LiveLatestObservationDocument>.Filter.Eq(
                static document => document.Id,
                incoming.Id),
            Builders<LiveLatestObservationDocument>.Filter.Eq(
                static document => document.SourceId,
                incoming.SourceId),
            Builders<LiveLatestObservationDocument>.Filter.Eq(
                "target.type",
                incoming.Target.Type.ToString()),
            Builders<LiveLatestObservationDocument>.Filter.Eq(
                "target.id",
                incoming.Target.Id));
    }

    public static UpdateDefinition<LiveLatestObservationDocument> BuildMonotonicUpdate(
        LiveLatestObservationDocument incoming)
    {
        BsonDocument serialized = incoming.ToBsonDocument();
        BsonValue observedAt = serialized["provenance"].AsBsonDocument["observedAtUtc"];
        BsonValue receivedAt = serialized["provenance"].AsBsonDocument["receivedAtUtc"];
        BsonDocument isNewer = new BsonDocument("$or", new BsonArray
        {
            new BsonDocument("$eq", new BsonArray
            {
                new BsonDocument("$type", "$provenance.observedAtUtc"),
                "missing",
            }),
            new BsonDocument("$lt", new BsonArray { "$provenance.observedAtUtc", observedAt }),
            new BsonDocument("$and", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray { "$provenance.observedAtUtc", observedAt }),
                new BsonDocument("$lt", new BsonArray { "$provenance.receivedAtUtc", receivedAt }),
            }),
        });
        BsonDocument values = new BsonDocument
        {
            ["createdAt"] = new BsonDocument(
                "$ifNull",
                new BsonArray { "$createdAt", serialized["createdAt"] }),
        };
        string[] replaceableFields =
        {
            "sourceId",
            "target",
            "status",
            "queues",
            "provenance",
            "freshnessPolicy",
            "expiresAtUtc",
            "payloadSha256",
            "hasStatusQueueConflict",
            "updatedAt",
        };
        foreach (string field in replaceableFields)
        {
            BsonValue incomingValue = serialized.TryGetValue(field, out BsonValue? value)
                ? value
                : BsonNull.Value;
            values[field] = new BsonDocument(
                "$cond",
                new BsonArray { "$_incomingIsNewer", incomingValue, $"${field}" });
        }

        return new PipelineUpdateDefinition<LiveLatestObservationDocument>(new BsonDocument[]
        {
            new BsonDocument("$set", new BsonDocument("_incomingIsNewer", isNewer)),
            new BsonDocument("$set", values),
            new BsonDocument("$unset", "_incomingIsNewer"),
        });
    }
}
