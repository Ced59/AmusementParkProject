using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class PassportProfileShareScopeRegistrationMongoDefinitions
{
    public static IReadOnlyCollection<
        CreateIndexModel<PassportProfileShareScopeRegistrationDocument>> BuildIndexes()
    {
        IndexKeysDefinition<PassportProfileShareScopeRegistrationDocument> dependencies =
            Builders<PassportProfileShareScopeRegistrationDocument>.IndexKeys
                .Ascending(document => document.OwnerUserId)
                .Ascending(document => document.ParkId)
                .Ascending(document => document.SelectedYears);
        return new[]
        {
            new CreateIndexModel<PassportProfileShareScopeRegistrationDocument>(
                dependencies,
                new CreateIndexOptions
                {
                    Name = "passport_profile_scope_segment",
                }),
        };
    }
}
