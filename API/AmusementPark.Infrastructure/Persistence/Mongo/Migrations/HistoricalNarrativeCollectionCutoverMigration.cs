using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

/// <summary>
/// Bascule une seule fois les narratifs historiques vers leur collection canonique.
/// Les anciennes collections ne sont supprimées qu'après la canonicalisation réussie.
/// </summary>
public sealed class HistoricalNarrativeCollectionCutoverMigration
{
    private readonly IMongoDatabase database;
    private readonly MongoDbSettings settings;
    private readonly ILogger<HistoricalNarrativeCollectionCutoverMigration> logger;

    public HistoricalNarrativeCollectionCutoverMigration(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<HistoricalNarrativeCollectionCutoverMigration> logger)
    {
        this.database = database ?? throw new ArgumentNullException(nameof(database));
        this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<long> StageAsync(CancellationToken cancellationToken)
    {
        HashSet<string> collectionNames = await this.GetCollectionNamesAsync(cancellationToken);
        IMongoCollection<BsonDocument> destination = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalNarrativesCollectionName);
        long importedCount = 0;

        importedCount += await this.StageCollectionAsync(
            collectionNames,
            this.settings.HistoryEventsCollectionName,
            destination,
            cancellationToken);
        importedCount += await this.StageCollectionAsync(
            collectionNames,
            this.settings.HistoricalEventsBackupCollectionName,
            destination,
            cancellationToken);

        if (importedCount > 0)
        {
            this.logger.LogInformation(
                "Staged {NarrativeCount} historical narratives for canonicalization.",
                importedCount);
        }

        return importedCount;
    }

    public async Task CompleteAsync(CancellationToken cancellationToken)
    {
        HashSet<string> collectionNames = await this.GetCollectionNamesAsync(cancellationToken);
        string[] supersededCollections =
        {
            this.settings.HistoryEventsCollectionName,
            this.settings.HistoricalEventsBackupCollectionName,
            this.settings.HistoricalMigrationsCollectionName,
            this.settings.HistoricalMigrationAnomaliesCollectionName,
        };
        foreach (string collectionName in supersededCollections
                     .Where(collectionNames.Contains)
                     .Distinct(StringComparer.Ordinal))
        {
            await this.database.DropCollectionAsync(collectionName, cancellationToken);
            this.logger.LogInformation(
                "Removed superseded historical collection {CollectionName} after canonical cutover.",
                collectionName);
        }
    }

    internal static BsonDocument PrepareNarrative(BsonDocument source)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (!source.Contains("_id"))
        {
            throw new InvalidOperationException("A historical narrative requires an identifier.");
        }

        BsonDocument narrative = source.DeepClone().AsBsonDocument;
        narrative["canonicalizationState"] =
            HistoricalNarrativeCanonicalizationState.PendingReview.ToString();
        narrative.Remove("migrationVersion");
        narrative.Remove("migrationWarnings");
        return narrative;
    }

    private async Task<long> StageCollectionAsync(
        IReadOnlySet<string> collectionNames,
        string sourceCollectionName,
        IMongoCollection<BsonDocument> destination,
        CancellationToken cancellationToken)
    {
        if (!collectionNames.Contains(sourceCollectionName)
            || string.Equals(
                sourceCollectionName,
                this.settings.HistoricalNarrativesCollectionName,
                StringComparison.Ordinal))
        {
            return 0;
        }

        IMongoCollection<BsonDocument> source = this.database.GetCollection<BsonDocument>(
            sourceCollectionName);
        long importedCount = 0;
        using IAsyncCursor<BsonDocument> cursor = await source
            .Find(Builders<BsonDocument>.Filter.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument sourceDocument in cursor.Current)
            {
                BsonDocument narrative = PrepareNarrative(sourceDocument);
                BsonValue identifier = narrative["_id"];
                BsonDocument valuesToInsert = narrative.DeepClone().AsBsonDocument;
                valuesToInsert.Remove("_id");
                UpdateResult result = await destination.UpdateOneAsync(
                    Builders<BsonDocument>.Filter.Eq("_id", identifier),
                    new BsonDocument("$setOnInsert", valuesToInsert),
                    new UpdateOptions { IsUpsert = true },
                    cancellationToken);
                if (result.UpsertedId is not null)
                {
                    importedCount++;
                }
            }
        }

        return importedCount;
    }

    private async Task<HashSet<string>> GetCollectionNamesAsync(CancellationToken cancellationToken)
    {
        using IAsyncCursor<string> cursor = await this.database.ListCollectionNamesAsync(
            cancellationToken: cancellationToken);
        List<string> names = await cursor.ToListAsync(cancellationToken);
        return names.ToHashSet(StringComparer.Ordinal);
    }
}
