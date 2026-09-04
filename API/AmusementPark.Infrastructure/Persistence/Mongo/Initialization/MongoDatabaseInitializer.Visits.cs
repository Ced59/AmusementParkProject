using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Repositories;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Initialization;

public sealed partial class MongoDatabaseInitializer
{
    private async Task InitializeUserVisitIndexesAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<UserVisitDocument> collection =
            this.database.GetCollection<UserVisitDocument>(this.settings.UserVisitsCollectionName);
        await collection.UpdateManyAsync(
            UserVisitMongoDefinitions.BuildMissingDateSortKeyFilter(),
            UserVisitMongoDefinitions.BuildDateSortKeyBackfillUpdate(),
            cancellationToken: cancellationToken);
        await collection.Indexes.CreateManyAsync(
            UserVisitMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializeUserRideOccurrenceIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<UserRideOccurrenceDocument> collection =
            this.database.GetCollection<UserRideOccurrenceDocument>(
                this.settings.UserRideOccurrencesCollectionName);
        await collection.Indexes.CreateManyAsync(
            UserRideOccurrenceMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializeUserRideOccurrenceOperationIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> collection =
            this.database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                this.settings.UserRideOccurrenceOperationsCollectionName);
        await collection.Indexes.CreateManyAsync(
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildIndexes(),
            cancellationToken);
    }

    private async Task InitializePassportAuditIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<PassportAuditJournalDocument> collection =
            this.database.GetCollection<PassportAuditJournalDocument>(
                this.settings.PassportAuditEventsCollectionName);
        await collection.Indexes.CreateManyAsync(
            PassportAuditMongoDefinitions.BuildJournalIndexes(),
            cancellationToken);
    }

    private async Task InitializeGlobalRatingSuggestionIndexesAsync(
        CancellationToken cancellationToken)
    {
        IMongoCollection<GlobalRatingSuggestionStateDocument> states =
            this.database.GetCollection<GlobalRatingSuggestionStateDocument>(
                this.settings.GlobalRatingSuggestionStatesCollectionName);
        await states.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildStateIndexes(),
            cancellationToken);

        IMongoCollection<GlobalRatingSuggestionPreferenceDocument> preferences =
            this.database.GetCollection<GlobalRatingSuggestionPreferenceDocument>(
                this.settings.GlobalRatingSuggestionPreferencesCollectionName);
        await preferences.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildPreferenceIndexes(),
            cancellationToken);

        IMongoCollection<GlobalRatingSuggestionInteractionDocument> interactions =
            this.database.GetCollection<GlobalRatingSuggestionInteractionDocument>(
                this.settings.GlobalRatingSuggestionInteractionsCollectionName);
        await interactions.Indexes.CreateManyAsync(
            GlobalRatingSuggestionMongoDefinitions.BuildInteractionIndexes(),
            cancellationToken);
    }
}
