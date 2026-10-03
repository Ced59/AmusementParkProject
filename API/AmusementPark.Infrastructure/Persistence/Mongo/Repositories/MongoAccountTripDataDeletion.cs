using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using static AmusementPark.Infrastructure.Persistence.Mongo.Repositories.MongoAccountDeletionDocumentStore;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountTripDataDeletion
{
    private const string DeletedAccountActor = "deleted-account";
    private readonly MongoAccountDeletionDocumentStore documents;
    private readonly MongoDbSettings settings;

    public MongoAccountTripDataDeletion(
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
        string[] ownedTripIds = await this.documents.ListIdsAsync(
            this.settings.TripPlansCollectionName,
            Eq("ownerUserId", userId),
            cancellationToken);
        if (ownedTripIds.Length > 0)
        {
            string[] childCollections =
            {
                this.settings.TripParkCandidatesCollectionName,
                this.settings.TripDayPlansCollectionName,
                this.settings.TripItemPreferencesCollectionName,
                this.settings.TripItemDecisionsCollectionName,
                this.settings.TripInvitationsCollectionName,
                this.settings.TripAuditEventsCollectionName,
                this.settings.TripNotificationSubscriptionsCollectionName,
            };
            foreach (string collectionName in childCollections)
            {
                deletedCount += await this.documents.DeleteManyAsync(
                    collectionName,
                    Builders<BsonDocument>.Filter.In("tripPlanId", ownedTripIds),
                    cancellationToken);
            }

            deletedCount += await this.documents.DeleteManyAsync(
                this.settings.TripPlansCollectionName,
                Builders<BsonDocument>.Filter.In("_id", ownedTripIds),
                cancellationToken);
        }

        deletedCount += await this.documents.DeleteManyAsync(
            this.settings.TripItemPreferencesCollectionName,
            Eq("userId", userId),
            cancellationToken);
        deletedCount += await this.documents.DeleteManyAsync(
            this.settings.TripNotificationSubscriptionsCollectionName,
            Eq("userId", userId),
            cancellationToken);

        await this.documents.Collection(this.settings.TripPlansCollectionName)
            .UpdateManyAsync(
                Eq("members.userId", userId),
                Builders<BsonDocument>.Update
                    .PullFilter("members", Eq("userId", userId))
                    .PullFilter("creationSnapshot.members", Eq("userId", userId))
                    .Pull("departedPreferenceCleanupUserIds", userId)
                    .Unset("memberAdmissionFence"),
                cancellationToken: cancellationToken);
        await this.documents.Collection(this.settings.TripPlansCollectionName)
            .UpdateManyAsync(
                Eq("memberAdmissionFence.candidateUserId", userId),
                Builders<BsonDocument>.Update.Unset("memberAdmissionFence"),
                cancellationToken: cancellationToken);
        await this.documents.Collection(this.settings.TripItemDecisionsCollectionName)
            .UpdateManyAsync(
                Eq("decidedByUserId", userId),
                Builders<BsonDocument>.Update.Set(
                    "decidedByUserId",
                    DeletedAccountActor),
                cancellationToken: cancellationToken);
        await this.documents.Collection(this.settings.TripInvitationsCollectionName)
            .UpdateManyAsync(
                Eq("acceptingUserId", userId),
                Builders<BsonDocument>.Update
                    .Unset("acceptingUserId")
                    .Unset("acceptanceOperationId")
                    .Unset("acceptanceOperationKeyHash"),
                cancellationToken: cancellationToken);
        return deletedCount;
    }
}
