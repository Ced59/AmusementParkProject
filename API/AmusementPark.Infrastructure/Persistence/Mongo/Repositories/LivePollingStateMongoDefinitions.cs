using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class LivePollingStateMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<LivePollingStateDocument>> BuildIndexes()
    {
        return new CreateIndexModel<LivePollingStateDocument>[]
        {
            new(
                Builders<LivePollingStateDocument>.IndexKeys
                    .Ascending(static document => document.SourceId),
                new CreateIndexOptions
                {
                    Name = "idx_live_polling_source_unique",
                    Unique = true,
                }),
            new(
                Builders<LivePollingStateDocument>.IndexKeys
                    .Ascending(static document => document.NextAttemptAtUtc)
                    .Ascending(static document => document.CircuitOpenUntilUtc)
                    .Ascending(static document => document.LeaseExpiresAtUtc),
                new CreateIndexOptions { Name = "idx_live_polling_due_lease" }),
        };
    }
}
