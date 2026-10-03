using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using static AmusementPark.Infrastructure.Persistence.Mongo.Repositories.MongoAccountDeletionDocumentStore;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountOperationalDataDeletion
{
    private const string DeletedAccountActor = "deleted-account";
    private readonly MongoAccountDeletionDocumentStore documents;
    private readonly MongoDbSettings settings;

    public MongoAccountOperationalDataDeletion(
        MongoAccountDeletionDocumentStore documents,
        MongoDbSettings settings)
    {
        this.documents = documents;
        this.settings = settings;
    }

    public async Task<long> PurgeAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        long deletedCount = 0;
        (string Collection, string Field)[] ownedDocuments =
        {
            (this.settings.UserGroupProfilesCollectionName, "ownerUserId"),
            (this.settings.CommentsCollectionName, "authorUserId"),
            (this.settings.HistoricalExistenceReportsCollectionName, "ownerUserId"),
            (this.settings.SocialShareEventsCollectionName, "userId"),
            (this.settings.ParkDataEditorAccessTokensCollectionName, "userId"),
        };
        foreach ((string collectionName, string field) in ownedDocuments)
        {
            deletedCount += await this.documents.DeleteManyAsync(
                collectionName,
                Eq(field, userId),
                cancellationToken);
        }

        await this.AnonymizeReferencesAsync(userId, cancellationToken);
        return deletedCount;
    }

    private async Task AnonymizeReferencesAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        await this.documents.AnonymizeAsync(
            this.settings.AdminAuditLogsCollectionName,
            "actorUserId",
            userId,
            Builders<BsonDocument>.Update
                .Unset("actorUserId")
                .Unset("actorEmail")
                .Unset("ipAddress")
                .Unset("userAgent")
                .Set("actorRoles", new BsonArray()),
            cancellationToken);

        (string Collection, string Field, bool Required)[] references =
        {
            (this.settings.HistoricalExistenceReportsCollectionName, "reviewedByUserId", false),
            (this.settings.ShareModerationReportsCollectionName, "reviewedByUserId", false),
            (this.settings.ParkFitSourceReportsCollectionName, "reviewedByUserId", false),
            (this.settings.LiveTargetMappingsCollectionName, "reviewedByUserId", false),
            (this.settings.LiveQualityIncidentsCollectionName, "resolvedByUserId", false),
            (this.settings.ParkGraphUpsertHistoryCollectionName, "requestedByUserId", false),
            (this.settings.SocialPublicationsCollectionName, "requestedByUserId", false),
            (this.settings.ParkDataEditorAccessTokensCollectionName, "revokedByUserId", false),
            (this.settings.FeatureFlagStatesCollectionName, "changedByUserId", true),
            (this.settings.LiveOperationalControlsCollectionName, "changedByUserId", true),
        };
        foreach ((string collectionName, string field, bool required) in references)
        {
            UpdateDefinition<BsonDocument> update = required
                ? Builders<BsonDocument>.Update.Set(field, DeletedAccountActor)
                : Builders<BsonDocument>.Update.Unset(field);
            await this.documents.AnonymizeAsync(
                collectionName,
                field,
                userId,
                update,
                cancellationToken);
        }

        await this.documents.Collection(
                this.settings.SeoSitemapGenerationHistoryCollectionName)
            .UpdateManyAsync(
                Eq("triggeredByUserId", userId),
                Builders<BsonDocument>.Update
                    .Unset("triggeredByUserId")
                    .Unset("triggeredByUserEmail"),
                cancellationToken: cancellationToken);
        await this.documents.Collection(
                this.settings.ParkFitOperationalStatusesCollectionName)
            .UpdateManyAsync(
                Eq("decisions.actorUserId", userId),
                Builders<BsonDocument>.Update.Set(
                    "decisions.$[decision].actorUserId",
                    DeletedAccountActor),
                new UpdateOptions
                {
                    ArrayFilters = new[]
                    {
                        new BsonDocumentArrayFilterDefinition<BsonDocument>(
                            new BsonDocument("decision.actorUserId", userId)),
                    },
                },
                cancellationToken);

        foreach (string collectionName in new[]
        {
            this.settings.HistoricalFactsCollectionName,
            this.settings.HistoricalSourcesCollectionName,
        })
        {
            await this.documents.AnonymizeAsync(
                collectionName,
                "transitionReviewEvent.actorUserId",
                userId,
                Builders<BsonDocument>.Update.Set(
                    "transitionReviewEvent.actorUserId",
                    DeletedAccountActor),
                cancellationToken);
        }
    }
}
