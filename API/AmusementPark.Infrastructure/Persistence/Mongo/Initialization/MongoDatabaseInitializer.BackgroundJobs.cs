using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.BackgroundJobs;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeDurableBackgroundJobIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<DurableBackgroundJobDocument> collection =
            this.database.GetCollection<DurableBackgroundJobDocument>(this.settings.DurableBackgroundJobsCollectionName);
        await collection.Indexes.CreateManyAsync(BuildDurableBackgroundJobIndexes(), cancellationToken);
    }

    internal static IReadOnlyCollection<CreateIndexModel<DurableBackgroundJobDocument>> BuildDurableBackgroundJobIndexes()
    {
        BsonDocument exactJobFilter = new BsonDocument(
            "idempotencyKey",
            new BsonDocument("$type", "string"));
        BsonDocument activeCoalescibleJobFilter = new BsonDocument
        {
            { "naturalKey", new BsonDocument("$type", "string") },
            {
                "status",
                new BsonDocument("$in", new BsonArray
                {
                    DurableBackgroundJobStatus.Pending.ToString(),
                    DurableBackgroundJobStatus.Leased.ToString(),
                    DurableBackgroundJobStatus.RetryScheduled.ToString(),
                })
            },
        };
        BsonDocument scheduledJobFilter = new BsonDocument(
            "status",
            new BsonDocument("$in", new BsonArray
            {
                DurableBackgroundJobStatus.Pending.ToString(),
                DurableBackgroundJobStatus.RetryScheduled.ToString(),
            }));
        BsonDocument leasedJobFilter = new BsonDocument(
            "status",
            DurableBackgroundJobStatus.Leased.ToString());

        return new List<CreateIndexModel<DurableBackgroundJobDocument>>
        {
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.IdempotencyKey),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_exact_unique",
                    Unique = true,
                    PartialFilterExpression = exactJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.NaturalKey),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_active_natural_key_unique",
                    Unique = true,
                    PartialFilterExpression = activeCoalescibleJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Descending(item => item.Priority)
                    .Ascending(item => item.NotBeforeUtc)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_runnable",
                    PartialFilterExpression = scheduledJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Descending(item => item.Priority)
                    .Ascending(item => item.LeaseExpiresAtUtc)
                    .Ascending(item => item.CreatedAt),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_expired_claim",
                    PartialFilterExpression = leasedJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys.Ascending(item => item.LeaseExpiresAtUtc),
                new CreateIndexOptions<DurableBackgroundJobDocument>
                {
                    Name = "idx_background_jobs_lease_expiry",
                    PartialFilterExpression = leasedJobFilter,
                }),
            new CreateIndexModel<DurableBackgroundJobDocument>(
                Builders<DurableBackgroundJobDocument>.IndexKeys
                    .Ascending(item => item.Kind)
                    .Ascending(item => item.Status)
                    .Descending(item => item.UpdatedAt),
                new CreateIndexOptions { Name = "idx_background_jobs_diagnostics" }),
        };
    }
}
