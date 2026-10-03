using AmusementPark.Infrastructure.Configuration.Mongo;
using MongoDB.Bson;
using MongoDB.Driver;
using static AmusementPark.Infrastructure.Persistence.Mongo.Repositories.MongoAccountDeletionDocumentStore;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class MongoAccountPassportDataDeletion
{
    private readonly MongoAccountDeletionDocumentStore documents;
    private readonly MongoDbSettings settings;

    public MongoAccountPassportDataDeletion(
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
        string[] exportIds = await this.documents.ListIdsAsync(
            this.settings.PassportExportsCollectionName,
            Eq("userId", userId),
            cancellationToken);
        if (exportIds.Length > 0)
        {
            deletedCount += await this.documents.DeleteManyAsync(
                this.settings.PassportExportChunksCollectionName,
                Builders<BsonDocument>.Filter.In("exportId", exportIds),
                cancellationToken);
        }

        string[] collectionNames =
        {
            this.settings.UserRideOccurrencesCollectionName,
            this.settings.UserRideOccurrenceOperationsCollectionName,
            this.settings.PassportAuditEventsCollectionName,
            this.settings.PassportExportsCollectionName,
            this.settings.GlobalRatingSuggestionStatesCollectionName,
            this.settings.GlobalRatingSuggestionPreferencesCollectionName,
            this.settings.UserRatingsCollectionName,
            this.settings.UserVisitsCollectionName,
        };
        foreach (string collectionName in collectionNames)
        {
            deletedCount += await this.documents.DeleteManyAsync(
                collectionName,
                Eq("userId", userId),
                cancellationToken);
        }

        return deletedCount;
    }
}
