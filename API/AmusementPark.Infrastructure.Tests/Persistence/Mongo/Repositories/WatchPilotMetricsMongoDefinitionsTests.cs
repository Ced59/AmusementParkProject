using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class WatchPilotMetricsMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_BoundsAggregateMetricRetention()
    {
        IReadOnlyCollection<CreateIndexModel<WatchPilotDailyMetricsDocument>> indexes =
            WatchPilotMetricsMongoDefinitions.BuildIndexes();

        CreateIndexModel<WatchPilotDailyMetricsDocument> retention = Assert.Single(indexes);
        Assert.Equal("ttl_watch_pilot_metrics", retention.Options.Name);
        Assert.Equal(TimeSpan.Zero, retention.Options.ExpireAfter);
    }
}
