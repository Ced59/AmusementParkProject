using AmusementPark.Core.Domain.Watchlists;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Watchlists;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserCollectionEntryMongoDefinitions
{
    public const string IdentityUniqueIndexName = "idx_user_collection_identity_unique";
    public const string OwnerSlotUniqueIndexName = "idx_user_collection_owner_slot_unique";
    public const string OwnerUpdatedIndexName = "idx_user_collection_owner_updated_id";

    public static FilterDefinition<UserCollectionEntryDocument> BuildOwnerFilter(string userId)
    {
        return Builders<UserCollectionEntryDocument>.Filter.Eq(
            static document => document.UserId,
            userId);
    }

    public static FilterDefinition<UserCollectionEntryDocument> BuildIdentityFilter(
        string userId,
        CollectionTargetType targetType,
        string targetId,
        UserCollectionKind kind)
    {
        return BuildOwnerFilter(userId)
            & Builders<UserCollectionEntryDocument>.Filter.Eq(
                static document => document.TargetType,
                targetType)
            & Builders<UserCollectionEntryDocument>.Filter.Eq(
                static document => document.TargetId,
                targetId)
            & Builders<UserCollectionEntryDocument>.Filter.Eq(
                static document => document.Kind,
                kind);
    }

    public static IReadOnlyCollection<CreateIndexModel<UserCollectionEntryDocument>> BuildIndexes()
    {
        CreateIndexModel<UserCollectionEntryDocument> identity = new(
            Builders<UserCollectionEntryDocument>.IndexKeys
                .Ascending(static document => document.UserId)
                .Ascending(static document => document.TargetType)
                .Ascending(static document => document.TargetId)
                .Ascending(static document => document.Kind),
            new CreateIndexOptions { Name = IdentityUniqueIndexName, Unique = true });
        CreateIndexModel<UserCollectionEntryDocument> ownerSlot = new(
            Builders<UserCollectionEntryDocument>.IndexKeys
                .Ascending(static document => document.UserId)
                .Ascending(static document => document.OwnerSlot),
            new CreateIndexOptions { Name = OwnerSlotUniqueIndexName, Unique = true });
        CreateIndexModel<UserCollectionEntryDocument> ownerUpdated = new(
            Builders<UserCollectionEntryDocument>.IndexKeys
                .Ascending(static document => document.UserId)
                .Descending(static document => document.UpdatedAt)
                .Ascending(static document => document.Id),
            new CreateIndexOptions { Name = OwnerUpdatedIndexName });
        return new[] { identity, ownerSlot, ownerUpdated };
    }
}
