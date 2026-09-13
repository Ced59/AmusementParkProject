using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class ProfileComparisonMongoDefinitions
{
    public const string ShareTokenUniqueIndexName = "idx_profile_comparison_share_token_unique";
    public const string InvitationUniqueIndexName = "idx_profile_comparison_invitation_unique";
    public const string LegacyCreatorIndexName = "idx_profile_comparison_creator_status_created";
    public const string LegacyAcceptorIndexName = "idx_profile_comparison_acceptor_status_created";
    public const string CreatorIndexName = "idx_profile_comparison_creator_status_created_id";
    public const string AcceptorIndexName = "idx_profile_comparison_acceptor_status_created_id";

    public static FilterDefinition<ProfileComparisonDocument> BuildIdFilter(string id)
    {
        return Builders<ProfileComparisonDocument>.Filter.Eq(
            static document => document.Id,
            id);
    }

    public static FilterDefinition<ProfileComparisonDocument> BuildShareTokenFilter(string token)
    {
        return Builders<ProfileComparisonDocument>.Filter.Eq(
            static document => document.ShareToken,
            token);
    }

    public static FilterDefinition<ProfileComparisonDocument> BuildActiveParticipantFilter(
        string userId)
    {
        FilterDefinition<ProfileComparisonDocument> participant =
            Builders<ProfileComparisonDocument>.Filter.Eq(
                static document => document.CreatorUserId,
                userId)
            | Builders<ProfileComparisonDocument>.Filter.Eq(
                static document => document.AcceptorUserId,
                userId);
        return participant & Builders<ProfileComparisonDocument>.Filter.Eq(
            static document => document.Status,
            ProfileComparisonStatus.Active);
    }

    public static FilterDefinition<ProfileComparisonDocument> BuildActiveParticipantPageFilter(
        string userId,
        ProfileComparisonListCursor? after)
    {
        FilterDefinition<ProfileComparisonDocument> active =
            BuildActiveParticipantFilter(userId);
        if (after is null)
        {
            return active;
        }

        FilterDefinitionBuilder<ProfileComparisonDocument> filters =
            Builders<ProfileComparisonDocument>.Filter;
        FilterDefinition<ProfileComparisonDocument> beforeCursor =
            filters.Lt(static document => document.CreatedAt, after.CreatedAtUtc)
            | filters.Eq(static document => document.CreatedAt, after.CreatedAtUtc)
                & filters.Lt(static document => document.Id, after.ComparisonId);
        return active & beforeCursor;
    }

    public static FilterDefinition<ProfileComparisonDocument> BuildVersionFilter(
        string id,
        long version)
    {
        return BuildIdFilter(id)
            & Builders<ProfileComparisonDocument>.Filter.Eq(
                static document => document.Version,
                version);
    }

    public static IReadOnlyCollection<CreateIndexModel<ProfileComparisonDocument>> BuildIndexes()
    {
        CreateIndexModel<ProfileComparisonDocument> shareToken = new(
            Builders<ProfileComparisonDocument>.IndexKeys.Ascending(
                static document => document.ShareToken),
            new CreateIndexOptions
            {
                Name = ShareTokenUniqueIndexName,
                Unique = true,
            });
        CreateIndexModel<ProfileComparisonDocument> invitation = new(
            Builders<ProfileComparisonDocument>.IndexKeys.Ascending(
                static document => document.InvitationId),
            new CreateIndexOptions
            {
                Name = InvitationUniqueIndexName,
                Unique = true,
            });
        CreateIndexModel<ProfileComparisonDocument> creator = new(
            Builders<ProfileComparisonDocument>.IndexKeys
                .Ascending(static document => document.CreatorUserId)
                .Ascending(static document => document.Status)
                .Descending(static document => document.CreatedAt)
                .Descending(static document => document.Id),
            new CreateIndexOptions { Name = CreatorIndexName });
        CreateIndexModel<ProfileComparisonDocument> acceptor = new(
            Builders<ProfileComparisonDocument>.IndexKeys
                .Ascending(static document => document.AcceptorUserId)
                .Ascending(static document => document.Status)
                .Descending(static document => document.CreatedAt)
                .Descending(static document => document.Id),
            new CreateIndexOptions { Name = AcceptorIndexName });
        return new[] { shareToken, invitation, creator, acceptor };
    }
}
