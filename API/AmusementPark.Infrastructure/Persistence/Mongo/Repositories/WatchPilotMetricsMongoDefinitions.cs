using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class WatchPilotMetricsMongoDefinitions
{
    internal static IReadOnlyCollection<CreateIndexModel<WatchPilotDailyMetricsDocument>> BuildIndexes()
    {
        return new[]
        {
            new CreateIndexModel<WatchPilotDailyMetricsDocument>(
                Builders<WatchPilotDailyMetricsDocument>.IndexKeys
                    .Ascending(static document => document.ExpiresAtUtc),
                new CreateIndexOptions
                {
                    ExpireAfter = TimeSpan.Zero,
                    Name = "ttl_watch_pilot_metrics",
                }),
        };
    }
}
