using AmusementPark.Core.Domain.LiveData;
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
                    .Ascending(static document => document.SourceId)
                    .Ascending("target.type")
                    .Ascending("target.id")
                    .Ascending(static document => document.UsagePolicyVersion)
                    .Ascending(static document => document.RetentionPolicyKey)
                    .Descending(static document => document.BucketStartUtc),
                new CreateIndexOptions { Name = "idx_live_history_bucket_statistics_v1" }),
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

    public static FilterDefinition<LiveHistoryBucketDocument> BuildStatisticsFilter(
        LiveDataSourceId sourceId,
        LiveTargetType targetType,
        string targetId,
        string usagePolicyVersion,
        string retentionPolicyKey,
        TimeSpan bucketDuration,
        DateTime fromUtc,
        DateTime toUtc)
    {
        if (bucketDuration <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(bucketDuration));
        }

        DateTime bucketLowerBoundUtc = fromUtc.Ticks > bucketDuration.Ticks
            ? fromUtc.Subtract(bucketDuration)
            : DateTime.SpecifyKind(DateTime.MinValue, DateTimeKind.Utc);
        return Builders<LiveHistoryBucketDocument>.Filter.Eq(
                static document => document.SourceId,
                sourceId.Value)
            & Builders<LiveHistoryBucketDocument>.Filter.Eq(
                static document => document.Target.Type,
                targetType)
            & Builders<LiveHistoryBucketDocument>.Filter.Eq(
                static document => document.Target.Id,
                targetId)
            & Builders<LiveHistoryBucketDocument>.Filter.Eq(
                static document => document.UsagePolicyVersion,
                usagePolicyVersion)
            & Builders<LiveHistoryBucketDocument>.Filter.Eq(
                static document => document.RetentionPolicyKey,
                retentionPolicyKey)
            & Builders<LiveHistoryBucketDocument>.Filter.Gte(
                static document => document.BucketStartUtc,
                bucketLowerBoundUtc)
            & Builders<LiveHistoryBucketDocument>.Filter.Lt(
                static document => document.BucketStartUtc,
                toUtc)
            & Builders<LiveHistoryBucketDocument>.Filter.Gt(
                static document => document.BucketEndUtc,
                fromUtc);
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
