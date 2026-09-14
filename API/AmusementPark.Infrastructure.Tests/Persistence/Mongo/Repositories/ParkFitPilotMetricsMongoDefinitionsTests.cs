using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class ParkFitPilotMetricsMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldBoundRetentionAndSupportDateRanges()
    {
        IReadOnlyCollection<CreateIndexModel<
            AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit.ParkFitPilotDailyMetricsDocument>>
            indexes = ParkFitPilotMetricsMongoDefinitions.BuildIndexes();

        Assert.Contains(indexes, index => index.Options.Name
            == ParkFitPilotMetricsMongoDefinitions.DateIndexName);
        CreateIndexModel<
            AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit.ParkFitPilotDailyMetricsDocument>
            retention = Assert.Single(indexes, index => index.Options.Name
                == ParkFitPilotMetricsMongoDefinitions.RetentionIndexName);
        Assert.Equal(TimeSpan.Zero, retention.Options.ExpireAfter);
    }
}
