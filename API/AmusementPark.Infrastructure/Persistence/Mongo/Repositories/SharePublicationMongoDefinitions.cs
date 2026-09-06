using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class SharePublicationMongoDefinitions
{
    public const string ShareTokenUniqueIndexName = "idx_share_publication_token_unique";

    public const string OwnerLifecycleIndexName = "idx_share_publication_owner_type_updated";

    public const string ActiveOwnerSourceUniqueIndexName =
        "idx_share_publication_active_owner_source_unique";

    public static FilterDefinition<SharePublicationDocument> BuildOwnedFilter(
        string publicationId,
        string ownerUserId)
    {
        return Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.Id,
                publicationId)
            & Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.OwnerUserId,
                ownerUserId);
    }

    public static FilterDefinition<SharePublicationDocument> BuildOwnedSourceFilter(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey)
    {
        return Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.OwnerUserId,
                ownerUserId)
            & Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.Type,
                publicationType)
            & Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.SourceScopeKey,
                sourceScopeKey);
    }

    public static FilterDefinition<SharePublicationDocument> BuildActiveOwnedSourceFilter(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey)
    {
        return BuildOwnedSourceFilter(ownerUserId, publicationType, sourceScopeKey)
            & Builders<SharePublicationDocument>.Filter.In(
                static document => document.Status,
                new[]
                {
                    SharePublicationStatus.Draft,
                    SharePublicationStatus.Published,
                    SharePublicationStatus.NeedsReview,
                });
    }

    public static FilterDefinition<SharePublicationDocument> BuildResolvableTokenFilter(
        string shareToken)
    {
        return Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.ShareToken,
                shareToken)
            & Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.Status,
                SharePublicationStatus.Published)
            & Builders<SharePublicationDocument>.Filter.In(
                static document => document.Visibility,
                new[] { ShareVisibility.Unlisted, ShareVisibility.Public });
    }

    public static FilterDefinition<SharePublicationDocument> BuildOwnedVersionFilter(
        string publicationId,
        string ownerUserId,
        long expectedVersion)
    {
        return BuildOwnedFilter(publicationId, ownerUserId)
            & Builders<SharePublicationDocument>.Filter.Eq(
                static document => document.Version,
                expectedVersion);
    }

    public static IReadOnlyCollection<CreateIndexModel<SharePublicationDocument>> BuildIndexes()
    {
        CreateIndexOptions<SharePublicationDocument> tokenOptions =
            new CreateIndexOptions<SharePublicationDocument>
            {
                Name = ShareTokenUniqueIndexName,
                Unique = true,
                PartialFilterExpression = new BsonDocument(
                "shareToken",
                new BsonDocument("$type", "string")),
            };
        CreateIndexModel<SharePublicationDocument> token = new CreateIndexModel<SharePublicationDocument>(
            Builders<SharePublicationDocument>.IndexKeys.Ascending(
                static document => document.ShareToken),
            tokenOptions);
        CreateIndexModel<SharePublicationDocument> ownerLifecycle =
            new CreateIndexModel<SharePublicationDocument>(
                Builders<SharePublicationDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.Type)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = OwnerLifecycleIndexName });
        CreateIndexOptions<SharePublicationDocument> activeOwnerSourceOptions =
            new CreateIndexOptions<SharePublicationDocument>
            {
                Name = ActiveOwnerSourceUniqueIndexName,
                Unique = true,
                PartialFilterExpression = new BsonDocument(
                    "status",
                    new BsonDocument(
                        "$in",
                        new BsonArray
                        {
                            nameof(SharePublicationStatus.Draft),
                            nameof(SharePublicationStatus.Published),
                            nameof(SharePublicationStatus.NeedsReview),
                        })),
            };
        CreateIndexModel<SharePublicationDocument> ownerSource =
            new CreateIndexModel<SharePublicationDocument>(
                Builders<SharePublicationDocument>.IndexKeys
                    .Ascending(static document => document.OwnerUserId)
                    .Ascending(static document => document.Type)
                    .Ascending(static document => document.SourceScopeKey),
                activeOwnerSourceOptions);
        return new[] { token, ownerLifecycle, ownerSource };
    }
}
