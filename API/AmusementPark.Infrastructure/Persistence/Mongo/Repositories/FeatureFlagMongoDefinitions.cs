using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FeatureFlags;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public static class FeatureFlagMongoDefinitions
{
    public static IReadOnlyCollection<CreateIndexModel<FeatureFlagStateDocument>> BuildIndexes()
    {
        IndexKeysDefinitionBuilder<FeatureFlagStateDocument> keys =
            Builders<FeatureFlagStateDocument>.IndexKeys;
        return new[]
        {
            new CreateIndexModel<FeatureFlagStateDocument>(
                keys.Ascending(static document => document.Environment)
                    .Ascending(static document => document.Key)
                    .Ascending(static document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "ux_feature_flag_environment_key_revision",
                    Unique = true,
                }),
            new CreateIndexModel<FeatureFlagStateDocument>(
                keys.Ascending(static document => document.Environment)
                    .Ascending(static document => document.FeatureFlagId)
                    .Ascending(static document => document.Revision),
                new CreateIndexOptions
                {
                    Name = "ux_feature_flag_environment_id_revision",
                    Unique = true,
                }),
        };
    }
}
