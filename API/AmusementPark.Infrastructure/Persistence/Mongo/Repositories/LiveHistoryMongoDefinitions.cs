using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LiveHistoryMongoDefinitions
{
    public const int MaximumSamplesPerBucket = 24;

    public static IReadOnlyCollection<CreateIndexModel<LiveLatestObservationDocument>>
        BuildRawIndexes()
    {
        return new CreateIndexModel<LiveLatestObservationDocument>[]
        {
            new(
                Builders<LiveLatestObservationDocument>.IndexKeys
                    .Ascending("target.type")
                    .Ascending("target.id")
                    .Descending("provenance.observedAtUtcTicks"),
                new CreateIndexOptions { Name = "idx_live_history_raw_target_observed" }),
            new(
                Builders<LiveLatestObservationDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "idx_live_history_raw_expiration_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }

    public static IReadOnlyCollection<CreateIndexModel<LiveHistoryBucketDocument>>
        BuildBucketIndexes()
    {
        return new CreateIndexModel<LiveHistoryBucketDocument>[]
        {
            new(
                Builders<LiveHistoryBucketDocument>.IndexKeys
                    .Ascending("target.type")
                    .Ascending("target.id")
                    .Descending(static document => document.BucketStartUtc),
                new CreateIndexOptions { Name = "idx_live_history_bucket_target_start" }),
            new(
                Builders<LiveHistoryBucketDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    Name = "idx_live_history_bucket_expiration_ttl",
                    ExpireAfter = TimeSpan.Zero,
                }),
        };
    }

    public static UpdateDefinition<LiveHistoryBucketDocument> BuildBucketUpdate(
        LiveHistoryBucketDocument incoming)
    {
        ArgumentNullException.ThrowIfNull(incoming);
        BsonDocument serialized = incoming.ToBsonDocument();
        BsonDocument sample = serialized["samples"].AsBsonArray.Single().AsBsonDocument;
        BsonDocument candidateSamples = new BsonDocument("$concatArrays", new BsonArray
        {
            new BsonDocument("$filter", new BsonDocument
            {
                ["input"] = new BsonDocument(
                    "$ifNull",
                    new BsonArray { "$samples", new BsonArray() }),
                ["as"] = "sample",
                ["cond"] = new BsonDocument(
                    "$ne",
                    new BsonArray { "$$sample.sampleId", sample["sampleId"] }),
            }),
            new BsonArray { sample },
        });
        BsonDocument values = new BsonDocument
        {
            ["createdAt"] = new BsonDocument(
                "$ifNull",
                new BsonArray { "$createdAt", serialized["createdAt"] }),
            ["updatedAt"] = new BsonDocument("$cond", new BsonArray
            {
                new BsonDocument(
                    "$gt",
                    new BsonArray { "$updatedAt", serialized["updatedAt"] }),
                "$updatedAt",
                serialized["updatedAt"],
            }),
            ["sourceId"] = serialized["sourceId"],
            ["target"] = serialized["target"],
            ["bucketStartUtc"] = serialized["bucketStartUtc"],
            ["bucketEndUtc"] = serialized["bucketEndUtc"],
            ["bucketDurationMilliseconds"] = serialized["bucketDurationMilliseconds"],
            ["usagePolicyVersion"] = serialized["usagePolicyVersion"],
            ["retentionPolicyKey"] = serialized["retentionPolicyKey"],
            ["expiresAtUtc"] = serialized["expiresAtUtc"],
            ["samples"] = new BsonDocument(
                "$slice",
                new BsonArray { "$_candidateSamples", -MaximumSamplesPerBucket }),
            ["isTruncated"] = new BsonDocument("$or", new BsonArray
            {
                new BsonDocument(
                    "$ifNull",
                    new BsonArray { "$isTruncated", false }),
                new BsonDocument(
                    "$gt",
                    new BsonArray
                    {
                        new BsonDocument("$size", "$_candidateSamples"),
                        MaximumSamplesPerBucket,
                    }),
            }),
        };
        return new PipelineUpdateDefinition<LiveHistoryBucketDocument>(new BsonDocument[]
        {
            new BsonDocument("$set", new BsonDocument("_candidateSamples", candidateSamples)),
            new BsonDocument("$set", values),
            new BsonDocument("$unset", "_candidateSamples"),
        });
    }
}
