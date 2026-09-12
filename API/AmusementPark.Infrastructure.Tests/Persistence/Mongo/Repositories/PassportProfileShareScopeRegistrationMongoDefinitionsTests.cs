using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class PassportProfileShareScopeRegistrationMongoDefinitionsTests
{
    [Fact]
    public void BuildIndexes_ShouldSupportOwnerParkAndYearDependencyLookup()
    {
        CreateIndexModel<PassportProfileShareScopeRegistrationDocument> index = Assert.Single(
            PassportProfileShareScopeRegistrationMongoDefinitions.BuildIndexes());

        Assert.Equal("passport_profile_scope_segment", index.Options.Name);
        Assert.NotEqual(true, index.Options.Unique);
        Assert.Equal(
            new BsonDocument
            {
                { "ownerUserId", 1 },
                { "parkId", 1 },
                { "selectedYears", 1 },
            },
            Render(index.Keys));
    }

    private static BsonDocument Render(
        IndexKeysDefinition<PassportProfileShareScopeRegistrationDocument> keys)
    {
        IBsonSerializer<PassportProfileShareScopeRegistrationDocument> serializer =
            BsonSerializer.SerializerRegistry.GetSerializer<
                PassportProfileShareScopeRegistrationDocument>();
        return keys.Render(
            new RenderArgs<PassportProfileShareScopeRegistrationDocument>(
                serializer,
                BsonSerializer.SerializerRegistry));
    }
}
