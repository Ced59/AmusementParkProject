using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ProfileComparisonInvitationMongoDefinitions
{
    public const string TokenUniqueIndexName = "idx_profile_comparison_invitation_token_unique";
    public const string CreatorIndexName = "idx_profile_comparison_invitation_creator_created";
    public const string PurgeIndexName = "idx_profile_comparison_invitation_purge_ttl";

    public static FilterDefinition<ProfileComparisonInvitationDocument> BuildTokenFilter(
        string token)
    {
        return Builders<ProfileComparisonInvitationDocument>.Filter.Eq(
            static document => document.Token,
            token);
    }

    public static FilterDefinition<ProfileComparisonInvitationDocument> BuildVersionFilter(
        string id,
        long version)
    {
        return Builders<ProfileComparisonInvitationDocument>.Filter.Eq(
                static document => document.Id,
                id)
            & Builders<ProfileComparisonInvitationDocument>.Filter.Eq(
                static document => document.Version,
                version);
    }

    public static FilterDefinition<ProfileComparisonInvitationDocument> BuildAcceptedVersionFilter(
        string id,
        long version)
    {
        return BuildVersionFilter(id, version)
            & Builders<ProfileComparisonInvitationDocument>.Filter.Eq(
                static document => document.Status,
                ProfileComparisonInvitationStatus.Accepted);
    }

    public static IReadOnlyCollection<CreateIndexModel<ProfileComparisonInvitationDocument>>
        BuildIndexes()
    {
        CreateIndexModel<ProfileComparisonInvitationDocument> token = new(
            Builders<ProfileComparisonInvitationDocument>.IndexKeys.Ascending(
                static document => document.Token),
            new CreateIndexOptions
            {
                Name = TokenUniqueIndexName,
                Unique = true,
            });
        CreateIndexModel<ProfileComparisonInvitationDocument> creator = new(
            Builders<ProfileComparisonInvitationDocument>.IndexKeys
                .Ascending(static document => document.CreatorUserId)
                .Descending(static document => document.CreatedAt),
            new CreateIndexOptions { Name = CreatorIndexName });
        CreateIndexModel<ProfileComparisonInvitationDocument> purge = new(
            Builders<ProfileComparisonInvitationDocument>.IndexKeys.Ascending(
                static document => document.PurgeAtUtc),
            new CreateIndexOptions
            {
                Name = PurgeIndexName,
                ExpireAfter = TimeSpan.Zero,
            });
        return new[] { token, creator, purge };
    }
}
