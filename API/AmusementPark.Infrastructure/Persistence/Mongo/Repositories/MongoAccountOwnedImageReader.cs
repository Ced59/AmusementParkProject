using AmusementPark.Application.Features.Users.Models;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using static AmusementPark.Infrastructure.Persistence.Mongo.Repositories.MongoAccountDeletionDocumentStore;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountOwnedImageReader : IAccountOwnedImageReader
{
    private readonly MongoAccountDeletionDocumentStore documents;
    private readonly MongoDbSettings settings;

    public MongoAccountOwnedImageReader(
        MongoAccountDeletionDocumentStore documents,
        MongoDbSettings settings)
    {
        this.documents = documents;
        this.settings = settings;
    }

    public async Task<IReadOnlyCollection<AccountOwnedImage>> ListAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        string[] commentIds = await this.documents.ListIdsAsync(
            this.settings.CommentsCollectionName,
            Eq("authorUserId", userId),
            cancellationToken);
        FilterDefinition<BsonDocument> imageFilter = Or(
            Eq("draftOwnerId", userId),
            And(Eq("ownerType", "User"), Eq("ownerId", userId)),
            commentIds.Length == 0
                ? Builders<BsonDocument>.Filter.Where(_ => false)
                : And(
                    Builders<BsonDocument>.Filter.In(
                        "ownerType",
                        new[] { "Comment", "CommentDraft" }),
                    Builders<BsonDocument>.Filter.In("ownerId", commentIds)));
        List<BsonDocument> imageDocuments = await this.documents
            .Collection(this.settings.ImagesCollectionName)
            .Find(imageFilter)
            .Project(Builders<BsonDocument>.Projection
                .Include("_id")
                .Include("path"))
            .ToListAsync(cancellationToken);
        return imageDocuments
            .Select(static document => new AccountOwnedImage(
                document.GetValue("_id").AsString,
                document.TryGetValue("path", out BsonValue? pathValue)
                    && pathValue.IsString
                    ? pathValue.AsString
                    : null))
            .ToArray();
    }
}
