using AmusementPark.Infrastructure.Persistence.Mongo.Documents.LiveData;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class LiveOperationalControlMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectRevisionAndNaturalScope()
    {
        CreateIndexModel<LiveOperationalControlDocument>[] indexes =
            LiveOperationalControlMongoDefinitions.BuildIndexes().ToArray();

        Assert.Equal(2, indexes.Length);
        Assert.All(indexes, static index => Assert.True(index.Options.Unique));
        Assert.Contains(indexes, static index =>
            index.Options.Name == "ux_live_operational_control_revision");
        Assert.Contains(indexes, static index =>
            index.Options.Name == "ux_live_operational_scope_revision");
    }
}
