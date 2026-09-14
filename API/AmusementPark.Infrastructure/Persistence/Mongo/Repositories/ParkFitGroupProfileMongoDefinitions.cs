using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitGroupProfileMongoDefinitions
{
    public const string OwnerAliasUniqueIndexName = "idx_park_fit_group_profile_owner_alias_unique";
    public const string OwnerUpdatedIndexName = "idx_park_fit_group_profile_owner_updated_id";

    public static FilterDefinition<ParkFitGroupProfileDocument> BuildOwnedIdFilter(
        string id,
        string ownerUserId)
    {
        FilterDefinitionBuilder<ParkFitGroupProfileDocument> filters =
            Builders<ParkFitGroupProfileDocument>.Filter;
        return filters.Eq(static document => document.Id, id)
            & filters.Eq(static document => document.OwnerUserId, ownerUserId);
    }

    public static FilterDefinition<ParkFitGroupProfileDocument> BuildOwnedVersionFilter(
        string id,
        string ownerUserId,
        long version)
    {
        return BuildOwnedIdFilter(id, ownerUserId)
            & Builders<ParkFitGroupProfileDocument>.Filter.Eq(
                static document => document.Version,
                version);
    }

    public static IReadOnlyCollection<CreateIndexModel<ParkFitGroupProfileDocument>> BuildIndexes()
    {
        CreateIndexModel<ParkFitGroupProfileDocument> ownerAlias = new(
            Builders<ParkFitGroupProfileDocument>.IndexKeys
                .Ascending(static document => document.OwnerUserId)
                .Ascending(static document => document.NormalizedAlias),
            new CreateIndexOptions
            {
                Name = OwnerAliasUniqueIndexName,
                Unique = true,
            });
        CreateIndexModel<ParkFitGroupProfileDocument> ownerUpdated = new(
            Builders<ParkFitGroupProfileDocument>.IndexKeys
                .Ascending(static document => document.OwnerUserId)
                .Descending(static document => document.UpdatedAt)
                .Ascending(static document => document.Id),
            new CreateIndexOptions { Name = OwnerUpdatedIndexName });
        return new[] { ownerAlias, ownerUpdated };
    }
}
