using AmusementPark.Infrastructure.Persistence.Mongo.Documents.SocialPublishing;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeSocialPublicationIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<SocialPublicationDocument> collection = this.database
            .GetCollection<SocialPublicationDocument>(this.settings.SocialPublicationsCollectionName);
        List<CreateIndexModel<SocialPublicationDocument>> indexes = new List<CreateIndexModel<SocialPublicationDocument>>
        {
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Descending(static document => document.RequestedAtUtc),
                new CreateIndexOptions { Name = "idx_social_publications_requested_desc" }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Ascending(static document => document.DeduplicationKey),
                new CreateIndexOptions<SocialPublicationDocument>
                {
                    Name = "idx_social_publications_deduplication_unique",
                    Unique = true,
                    PartialFilterExpression = Builders<SocialPublicationDocument>.Filter.Type(
                        static document => document.DeduplicationKey,
                        MongoDB.Bson.BsonType.String),
                }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys
                    .Ascending(static document => document.Network)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.RequestedAtUtc),
                new CreateIndexOptions { Name = "idx_social_publications_network_status" }),
            new CreateIndexModel<SocialPublicationDocument>(
                Builders<SocialPublicationDocument>.IndexKeys.Ascending(static document => document.ExternalPostId),
                new CreateIndexOptions<SocialPublicationDocument>
                {
                    Name = "idx_social_publications_external_post_id",
                    PartialFilterExpression = Builders<SocialPublicationDocument>.Filter.Type(
                        static document => document.ExternalPostId,
                        MongoDB.Bson.BsonType.String),
                }),
        };

        await collection.Indexes.CreateManyAsync(indexes, cancellationToken: cancellationToken);
    }
}
