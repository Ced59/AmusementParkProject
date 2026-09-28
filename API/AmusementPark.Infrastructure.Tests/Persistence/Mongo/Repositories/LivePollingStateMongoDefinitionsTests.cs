using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LivePollingStateMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldKeepOnePilotStatePerSource()
    {
        IReadOnlyCollection<CreateIndexModel<LivePollingStateDocument>> indexes =
            LivePollingStateMongoDefinitions.BuildIndexes();

        CreateIndexModel<LivePollingStateDocument> unique = Assert.Single(
            indexes,
            static index => index.Options.Name == "idx_live_polling_source_unique");
        Assert.True(unique.Options.Unique);
        Assert.Contains(
            indexes,
            static index => index.Options.Name == "idx_live_polling_due_lease");
    }
}
