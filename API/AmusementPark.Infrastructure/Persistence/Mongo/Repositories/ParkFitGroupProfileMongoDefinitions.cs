using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ParkFitGroupProfileMongoDefinitions
{
    public const string OwnerAliasUniqueIndexName = "idx_park_fit_group_profile_owner_alias_unique";
    public const string OwnerSlotUniqueIndexName = "idx_park_fit_group_profile_owner_slot_unique";
    public const string OwnerUpdatedIndexName = "idx_park_fit_group_profile_owner_updated_id";

    public static FilterDefinition<ParkFitGroupProfileDocument> BuildOwnedIdFilter(
        string id,
        string ownerUserId)
    {
        return Builders<ParkFitGroupProfileDocument>.Filter.Eq(
                static document => document.Id,
                id)
            & BuildOwnerFilter(ownerUserId);
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

    public static FilterDefinition<ParkFitGroupProfileDocument> BuildOwnedAliasFilter(
        string ownerUserId,
        string normalizedAlias)
    {
        return BuildOwnerFilter(ownerUserId)
            & Builders<ParkFitGroupProfileDocument>.Filter.Eq(
                static document => document.NormalizedAlias,
                normalizedAlias);
    }

    public static FilterDefinition<ParkFitGroupProfileDocument> BuildOwnerFilter(
        string ownerUserId)
    {
        return Builders<ParkFitGroupProfileDocument>.Filter.Eq(
            static document => document.OwnerUserId,
            ownerUserId);
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
        CreateIndexModel<ParkFitGroupProfileDocument> ownerSlot = new(
            Builders<ParkFitGroupProfileDocument>.IndexKeys
                .Ascending(static document => document.OwnerUserId)
                .Ascending(static document => document.OwnerSlot),
            new CreateIndexOptions
            {
                Name = OwnerSlotUniqueIndexName,
                Unique = true,
            });
        return new[] { ownerAlias, ownerUpdated, ownerSlot };
    }
}
