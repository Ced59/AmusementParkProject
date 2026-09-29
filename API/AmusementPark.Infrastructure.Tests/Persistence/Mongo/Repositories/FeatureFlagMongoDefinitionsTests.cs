using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FeatureFlags;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class FeatureFlagMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldProtectEnvironmentKeyAndRevision()
    {
        IReadOnlyCollection<CreateIndexModel<FeatureFlagStateDocument>> indexes =
            FeatureFlagMongoDefinitions.BuildIndexes();

        Assert.Equal(2, indexes.Count);
        Assert.All(indexes, static index => Assert.True(index.Options.Unique));
        Assert.Contains(indexes, static index =>
            index.Options.Name == "ux_feature_flag_environment_key_revision");
        Assert.Contains(indexes, static index =>
            index.Options.Name == "ux_feature_flag_environment_id_revision");
    }
}
