using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using AmusementPark.Infrastructure.Persistence.Mongo.Initialization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Initialization;

public sealed class MongoDatabaseInitializerBackgroundJobsTests
{
    [Fact]
    public void BuildDurableBackgroundJobIndexes_ShouldProtectBothIdempotencyModes()
    {
        IReadOnlyCollection<CreateIndexModel<DurableBackgroundJobDocument>> indexes =
            MongoDatabaseInitializer.BuildDurableBackgroundJobIndexes();

        CreateIndexModel<DurableBackgroundJobDocument> exact = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_exact_unique", StringComparison.Ordinal));
        CreateIndexModel<DurableBackgroundJobDocument> coalescible = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_active_natural_key_unique", StringComparison.Ordinal));

        Assert.True(exact.Options.Unique);
        Assert.True(coalescible.Options.Unique);
        Assert.Equal(new BsonDocument { { "kind", 1 }, { "idempotencyKey", 1 } }, Render(exact.Keys));
        Assert.Equal(new BsonDocument { { "kind", 1 }, { "naturalKey", 1 } }, Render(coalescible.Keys));
        Assert.Contains("idempotencyKey", Render(exact.Options.PartialFilterExpression!).ToJson(), StringComparison.Ordinal);
        string coalescibleFilter = Render(coalescible.Options.PartialFilterExpression!).ToJson();
        Assert.Contains("naturalKey", coalescibleFilter, StringComparison.Ordinal);
        Assert.Contains("Pending", coalescibleFilter, StringComparison.Ordinal);
        Assert.Contains("Leased", coalescibleFilter, StringComparison.Ordinal);
        Assert.Contains("RetryScheduled", coalescibleFilter, StringComparison.Ordinal);
    }

    [Fact]
    public void BuildDurableBackgroundJobIndexes_ShouldSupportSchedulingAndDiagnostics()
    {
        IReadOnlyCollection<CreateIndexModel<DurableBackgroundJobDocument>> indexes =
            MongoDatabaseInitializer.BuildDurableBackgroundJobIndexes();

        CreateIndexModel<DurableBackgroundJobDocument> runnable = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_runnable", StringComparison.Ordinal));
        CreateIndexModel<DurableBackgroundJobDocument> expiredClaims = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_expired_claim", StringComparison.Ordinal));
        CreateIndexModel<DurableBackgroundJobDocument> leases = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_lease_expiry", StringComparison.Ordinal));
        CreateIndexModel<DurableBackgroundJobDocument> diagnostics = Assert.Single(
            indexes,
            static index => string.Equals(index.Options.Name, "idx_background_jobs_diagnostics", StringComparison.Ordinal));

        Assert.Equal(
            new BsonDocument { { "kind", 1 }, { "priority", -1 }, { "notBeforeUtc", 1 }, { "createdAt", 1 } },
            Render(runnable.Keys));
        Assert.Equal(
            new BsonDocument { { "kind", 1 }, { "priority", -1 }, { "leaseExpiresAtUtc", 1 }, { "createdAt", 1 } },
            Render(expiredClaims.Keys));
        Assert.Equal(new BsonDocument("leaseExpiresAtUtc", 1), Render(leases.Keys));
        Assert.Equal(new BsonDocument { { "kind", 1 }, { "status", 1 }, { "updatedAt", -1 } }, Render(diagnostics.Keys));

        string runnableFilter = Render(runnable.Options.PartialFilterExpression!).ToJson();
        Assert.Contains(DurableBackgroundJobStatus.Pending.ToString(), runnableFilter, StringComparison.Ordinal);
        Assert.Contains(DurableBackgroundJobStatus.RetryScheduled.ToString(), runnableFilter, StringComparison.Ordinal);
        Assert.Equal(
            DurableBackgroundJobStatus.Leased.ToString(),
            Render(expiredClaims.Options.PartialFilterExpression!)["status"].AsString);
        Assert.Equal(
            DurableBackgroundJobStatus.Leased.ToString(),
            Render(leases.Options.PartialFilterExpression!)["status"].AsString);
    }

    private static BsonDocument Render(IndexKeysDefinition<DurableBackgroundJobDocument> keys)
    {
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();
        RenderArgs<DurableBackgroundJobDocument> arguments =
            new RenderArgs<DurableBackgroundJobDocument>(serializer, BsonSerializer.SerializerRegistry);
        return keys.Render(arguments);
    }

    private static BsonDocument Render(FilterDefinition<DurableBackgroundJobDocument> filter)
    {
        IBsonSerializer<DurableBackgroundJobDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<DurableBackgroundJobDocument>();
        RenderArgs<DurableBackgroundJobDocument> arguments =
            new RenderArgs<DurableBackgroundJobDocument>(serializer, BsonSerializer.SerializerRegistry);
        return filter.Render(arguments);
    }
}
