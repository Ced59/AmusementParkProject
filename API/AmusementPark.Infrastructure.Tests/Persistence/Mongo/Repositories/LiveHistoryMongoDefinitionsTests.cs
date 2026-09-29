using AmusementPark.Core.Domain.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveHistoryMongoDefinitionsTests
{
    private static readonly DateTime ObservedAtUtc =
        new DateTime(2026, 9, 29, 12, 34, 0, DateTimeKind.Utc).AddTicks(7);
    private static readonly LiveHistoryRetentionPolicy RetentionPolicy =
        new LiveHistoryRetentionPolicy(
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(400),
            TimeSpan.FromHours(1));

    [Fact]
    public void BuildIndexes_ShouldExpireRawAndBucketDataIndependently()
    {
        CreateIndexModel<LiveLatestObservationDocument> rawTtl = Assert.Single(
            LiveHistoryMongoDefinitions.BuildRawIndexes(),
            static index => index.Options.Name == "idx_live_history_raw_expiration_ttl");
        CreateIndexModel<LiveHistoryBucketDocument> bucketTtl = Assert.Single(
            LiveHistoryMongoDefinitions.BuildBucketIndexes(),
            static index => index.Options.Name == "idx_live_history_bucket_expiration_ttl");

        Assert.Equal(TimeSpan.Zero, rawTtl.Options.ExpireAfter);
        Assert.Equal(TimeSpan.Zero, bucketTtl.Options.ExpireAfter);
    }

    [Fact]
    public void ToHistoryDocuments_ShouldApplyShortRawAndLongBucketRetention()
    {
        LiveLatestObservation observation = CreateObservation();

        LiveLatestObservationDocument raw = observation.ToRawHistoryDocument(RetentionPolicy);
        LiveHistoryBucketDocument bucket = observation.ToHistoryBucketDocument(RetentionPolicy);

        Assert.Equal(observation.Provenance.NormalizedAtUtc.AddDays(7), raw.ExpiresAtUtc);
        Assert.Equal(
            new DateTime(2026, 9, 29, 12, 0, 0, DateTimeKind.Utc),
            bucket.BucketStartUtc);
        Assert.Equal(bucket.BucketStartUtc.AddHours(1).AddDays(400), bucket.ExpiresAtUtc);
        LiveHistoryBucketSampleDocument sample = Assert.Single(bucket.Samples);
        Assert.Equal(ObservedAtUtc.Ticks, sample.ObservedAtUtcTicks);
        Assert.Equal(0, Assert.Single(sample.Queues).WaitTimeMinutes);
        Assert.NotEqual(observation.ToDocument().Id, raw.Id);
    }

    [Fact]
    public void BuildBucketUpdate_ShouldReplaceDuplicateAndCapHourlySamples()
    {
        LiveHistoryBucketDocument bucket = CreateObservation()
            .ToHistoryBucketDocument(RetentionPolicy);

        BsonValue rendered = LiveHistoryMongoDefinitions.BuildBucketUpdate(bucket).Render(
            new RenderArgs<LiveHistoryBucketDocument>(
                BsonSerializer.LookupSerializer<LiveHistoryBucketDocument>(),
                BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains("$filter", json, StringComparison.Ordinal);
        Assert.Contains("$slice", json, StringComparison.Ordinal);
        Assert.Contains("sampleId", json, StringComparison.Ordinal);
        Assert.Contains("isTruncated", json, StringComparison.Ordinal);
        Assert.Contains(
            (-LiveHistoryMongoDefinitions.MaximumSamplesPerBucket).ToString(
                System.Globalization.CultureInfo.InvariantCulture),
            json,
            StringComparison.Ordinal);
    }

    [Fact]
    public void ToHistoryBucketDocument_WhenPolicyChanges_ShouldUseSeparateBucket()
    {
        LiveHistoryBucketDocument previousPolicy = CreateObservation("usage-1")
            .ToHistoryBucketDocument(RetentionPolicy);
        LiveHistoryBucketDocument currentPolicy = CreateObservation("usage-2")
            .ToHistoryBucketDocument(RetentionPolicy);

        Assert.NotEqual(previousPolicy.Id, currentPolicy.Id);
        Assert.Equal("usage-1", previousPolicy.UsagePolicyVersion);
        Assert.Equal("usage-2", currentPolicy.UsagePolicyVersion);
    }

    [Fact]
    public void ToHistoryDocuments_WhenRetentionChanges_ShouldUseSeparateStorageKeys()
    {
        LiveLatestObservation observation = CreateObservation();
        LiveHistoryRetentionPolicy shorterBuckets = new LiveHistoryRetentionPolicy(
            TimeSpan.FromDays(7),
            TimeSpan.FromDays(400),
            TimeSpan.FromMinutes(30));

        LiveLatestObservationDocument originalRaw =
            observation.ToRawHistoryDocument(RetentionPolicy);
        LiveLatestObservationDocument changedRaw =
            observation.ToRawHistoryDocument(shorterBuckets);
        LiveHistoryBucketDocument originalBucket =
            observation.ToHistoryBucketDocument(RetentionPolicy);
        LiveHistoryBucketDocument changedBucket =
            observation.ToHistoryBucketDocument(shorterBuckets);

        Assert.NotEqual(originalRaw.Id, changedRaw.Id);
        Assert.NotEqual(originalBucket.Id, changedBucket.Id);
        Assert.NotEqual(originalBucket.RetentionPolicyKey, changedBucket.RetentionPolicyKey);
    }

    private static LiveLatestObservation CreateObservation(string usagePolicyVersion = "usage-1")
    {
        DateTime receivedAtUtc = ObservedAtUtc.AddSeconds(10);
        return new LiveLatestObservation(
            new LiveTargetReference(
                LiveTargetType.ParkItem,
                "item-1",
                "park-1",
                "Attraction",
                "Park",
                "FR"),
            LiveOperationalStatus.Open,
            new[] { new LiveQueueObservation(LiveQueueKind.Standby, 0, false) },
            new LiveObservationProvenance(
                LiveDataSourceId.Parse("source"),
                "external-1",
                ObservedAtUtc,
                receivedAtUtc,
                receivedAtUtc,
                "correlation",
                "adapter-1",
                "mapping-1",
                LiveDataConfidence.Medium,
                usagePolicyVersion,
                "transform-1"),
            new LiveFreshnessPolicy(
                "freshness-1",
                TimeSpan.FromMinutes(10),
                TimeSpan.FromMinutes(20),
                TimeSpan.FromMinutes(30),
                TimeSpan.FromMinutes(1)),
            new string('c', 64));
    }
}
