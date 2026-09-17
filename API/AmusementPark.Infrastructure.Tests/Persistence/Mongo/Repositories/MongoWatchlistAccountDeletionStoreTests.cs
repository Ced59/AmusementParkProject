using System.Text.Json;
using AmusementPark.Application.Features.Watchlists.Models;
using AmusementPark.Application.Features.Watchlists.Services;
using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class MongoWatchlistAccountDeletionStoreTests
{
    [Fact]
    public void IsOwnedDigestJob_AcceptsOnlyMatchingDigestOwner()
    {
        DurableBackgroundJobDocument document = new DurableBackgroundJobDocument
        {
            Kind = NotificationDigestJob.Kind,
            PayloadJson = JsonSerializer.Serialize(new NotificationDigestJobPayload(
                "user-1",
                NotificationChannel.Email,
                NotificationFrequency.DailyDigest,
                DateTime.UtcNow)),
        };

        Assert.True(MongoWatchlistAccountDeletionStore.IsOwnedDigestJob(document, "user-1"));
        Assert.False(MongoWatchlistAccountDeletionStore.IsOwnedDigestJob(document, "user-2"));
    }

    [Fact]
    public void IsOwnedDigestJob_RejectsOtherKindsAndMalformedPayloads()
    {
        DurableBackgroundJobDocument otherKind = new DurableBackgroundJobDocument
        {
            Kind = NotificationEmailDeliveryJob.Kind,
            PayloadJson = "{}",
        };
        DurableBackgroundJobDocument malformed = new DurableBackgroundJobDocument
        {
            Kind = NotificationDigestJob.Kind,
            PayloadJson = "not-json",
        };

        Assert.False(MongoWatchlistAccountDeletionStore.IsOwnedDigestJob(otherKind, "user-1"));
        Assert.False(MongoWatchlistAccountDeletionStore.IsOwnedDigestJob(malformed, "user-1"));
    }

    [Fact]
    public void BuildOwnedDigestJobCandidateFilter_ScopesDatabaseScanToOwnerPayload()
    {
        FilterDefinition<DurableBackgroundJobDocument> filter =
            MongoWatchlistAccountDeletionStore.BuildOwnedDigestJobCandidateFilter("user-1");
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();

        BsonDocument rendered = filter.Render(new RenderArgs<DurableBackgroundJobDocument>(
            serializer,
            BsonSerializer.SerializerRegistry));
        string json = rendered.ToJson();

        Assert.Contains(NotificationDigestJob.Kind, json, StringComparison.Ordinal);
        Assert.Contains("payload", json, StringComparison.Ordinal);
        Assert.Contains("user-1", json, StringComparison.Ordinal);
    }
}
