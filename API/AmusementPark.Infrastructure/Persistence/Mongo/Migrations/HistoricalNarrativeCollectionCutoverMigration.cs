using AmusementPark.Application.Features.History.Services;
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
    internal const string CutoverVersion = "hist-canonical-cutover-v1";
    internal const string PreviousCanonicalFactIdField = "cutoverPreviousCanonicalFactId";

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
        IMongoCollection<BsonDocument> narrativeBackup = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalNarrativeCutoverBackupCollectionName);
        FilterDefinition<BsonDocument> candidateFilter = BuildCandidateFilter();
        List<BsonDocument> existingCandidates = await this.BackupDocumentsAsync(
            destination,
            narrativeBackup,
            candidateFilter,
            cancellationToken);
        await MarkExistingCandidatesAsync(destination, existingCandidates, cancellationToken);
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
        importedCount += await this.StageCollectionAsync(
            collectionNames,
            this.settings.HistoricalFrozenSourceCollectionName,
            destination,
            cancellationToken);

        await this.BackupAffectedCanonicalDocumentsAsync(destination, cancellationToken);
        await this.MarkBackupStageCompleteAsync(cancellationToken);

        if (importedCount > 0)
        {
            this.logger.LogInformation(
                "Staged {NarrativeCount} historical narratives for canonicalization.",
                importedCount);
        }

        return importedCount;
    }

    private async Task MarkBackupStageCompleteAsync(CancellationToken cancellationToken)
    {
        IMongoCollection<BsonDocument> state = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalCutoverStateCollectionName);
        BsonDocument document = new BsonDocument
        {
            { "_id", CutoverVersion },
            { "backupStageCompletedAtUtc", DateTime.UtcNow },
        };
        await state.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", CutoverVersion),
            document,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
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
        narrative["cutoverVersion"] = CutoverVersion;
        if (narrative.TryGetValue("canonicalFactId", out BsonValue? canonicalFactId)
            && canonicalFactId.IsString
            && !string.IsNullOrWhiteSpace(canonicalFactId.AsString))
        {
            narrative[PreviousCanonicalFactIdField] = canonicalFactId.AsString;
        }

        narrative.Remove("migrationVersion");
        narrative.Remove("migrationWarnings");
        return narrative;
    }

    internal static FilterDefinition<BsonDocument> BuildCandidateFilter()
    {
        FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
        return builder.Ne("cutoverVersion", CutoverVersion)
            & (builder.Ne(
                "migrationVersion",
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion)
            | builder.Nin(
                "canonicalizationState",
                new[]
                {
                    HistoricalNarrativeCanonicalizationState.Canonicalized.ToString(),
                    HistoricalNarrativeCanonicalizationState.Blocked.ToString(),
                }));
    }

    private static async Task MarkExistingCandidatesAsync(
        IMongoCollection<BsonDocument> destination,
        IReadOnlyCollection<BsonDocument> candidates,
        CancellationToken cancellationToken)
    {
        if (candidates.Count == 0)
        {
            return;
        }

        List<WriteModel<BsonDocument>> writes = new List<WriteModel<BsonDocument>>(candidates.Count);
        foreach (BsonDocument candidate in candidates)
        {
            BsonValue identifier = candidate["_id"];
            UpdateDefinition<BsonDocument> update = Builders<BsonDocument>.Update
                .Set("cutoverVersion", CutoverVersion);
            if (candidate.TryGetValue("canonicalFactId", out BsonValue? canonicalFactId)
                && canonicalFactId.IsString
                && !string.IsNullOrWhiteSpace(canonicalFactId.AsString))
            {
                update = update.Set(PreviousCanonicalFactIdField, canonicalFactId.AsString);
            }
            else
            {
                update = update.Unset(PreviousCanonicalFactIdField);
            }

            writes.Add(new UpdateOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", identifier),
                update));
        }

        await destination.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = true },
            cancellationToken);
    }

    internal static FilterDefinition<BsonDocument> BuildAffectedFactFilter(
        IReadOnlyCollection<string> narrativeIds,
        IReadOnlyCollection<string> linkedFactIds)
    {
        ArgumentNullException.ThrowIfNull(narrativeIds);
        ArgumentNullException.ThrowIfNull(linkedFactIds);
        FilterDefinitionBuilder<BsonDocument> builder = Builders<BsonDocument>.Filter;
        List<FilterDefinition<BsonDocument>> filters = new List<FilterDefinition<BsonDocument>>(2);
        if (narrativeIds.Count > 0)
        {
            filters.Add(builder.In("narrativeContentId", narrativeIds));
        }

        if (linkedFactIds.Count > 0)
        {
            filters.Add(builder.In("factId", linkedFactIds));
        }

        if (filters.Count == 0)
        {
            return builder.Where(static _ => false);
        }

        return filters.Count == 1 ? filters[0] : builder.Or(filters);
    }

    private async Task BackupAffectedCanonicalDocumentsAsync(
        IMongoCollection<BsonDocument> narratives,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> stagedNarratives = await narratives
            .Find(Builders<BsonDocument>.Filter.Eq("cutoverVersion", CutoverVersion))
            .Project(new BsonDocument
            {
                { "_id", 1 },
                { PreviousCanonicalFactIdField, 1 },
            })
            .ToListAsync(cancellationToken);
        string[] narrativeIds = stagedNarratives
            .Where(static narrative => narrative.TryGetValue("_id", out BsonValue? value)
                && value.IsString
                && !string.IsNullOrWhiteSpace(value.AsString))
            .Select(static narrative => narrative["_id"].AsString)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        string[] linkedFactIds = stagedNarratives
            .Where(static narrative =>
                narrative.TryGetValue(PreviousCanonicalFactIdField, out BsonValue? value)
                && value.IsString
                && !string.IsNullOrWhiteSpace(value.AsString))
            .Select(static narrative => narrative[PreviousCanonicalFactIdField].AsString)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (narrativeIds.Length == 0 && linkedFactIds.Length == 0)
        {
            return;
        }

        IMongoCollection<BsonDocument> facts = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalFactsCollectionName);
        List<string> affectedFactIds = await facts
            .Distinct<string>(
                "factId",
                BuildAffectedFactFilter(narrativeIds, linkedFactIds))
            .ToListAsync(cancellationToken);
        string[] factIds = affectedFactIds
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (factIds.Length == 0)
        {
            return;
        }

        IMongoCollection<BsonDocument> factBackup = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalFactCutoverBackupCollectionName);
        List<string> backedFactIds = await factBackup
            .Distinct<string>(
                "factId",
                Builders<BsonDocument>.Filter.In("factId", factIds))
            .ToListAsync(cancellationToken);
        string[] factIdsToBackup = factIds
            .Except(backedFactIds, StringComparer.Ordinal)
            .ToArray();
        if (factIdsToBackup.Length > 0)
        {
            await this.BackupDocumentsAsync(
                facts,
                factBackup,
                Builders<BsonDocument>.Filter.In("factId", factIdsToBackup),
                cancellationToken);
        }

        List<BsonDocument> backedFacts = await factBackup
            .Find(Builders<BsonDocument>.Filter.In("factId", factIds))
            .ToListAsync(cancellationToken);
        string[] sourceIds = backedFacts
            .Where(static fact => fact.TryGetValue("sources", out BsonValue? value) && value.IsBsonArray)
            .SelectMany(static fact => fact["sources"].AsBsonArray)
            .Where(static source =>
                source.IsBsonDocument
                && source.AsBsonDocument.TryGetValue("sourceId", out BsonValue? value)
                && value.IsString
                && !string.IsNullOrWhiteSpace(value.AsString))
            .Select(static source => source.AsBsonDocument["sourceId"].AsString)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (sourceIds.Length == 0)
        {
            return;
        }

        IMongoCollection<BsonDocument> sources = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalSourcesCollectionName);
        IMongoCollection<BsonDocument> sourceBackup = this.database.GetCollection<BsonDocument>(
            this.settings.HistoricalSourceCutoverBackupCollectionName);
        List<string> backedSourceIds = await sourceBackup
            .Distinct<string>(
                "sourceId",
                Builders<BsonDocument>.Filter.In("sourceId", sourceIds))
            .ToListAsync(cancellationToken);
        string[] sourceIdsToBackup = sourceIds
            .Except(backedSourceIds, StringComparer.Ordinal)
            .ToArray();
        if (sourceIdsToBackup.Length > 0)
        {
            await this.BackupDocumentsAsync(
                sources,
                sourceBackup,
                Builders<BsonDocument>.Filter.In("sourceId", sourceIdsToBackup),
                cancellationToken);
        }
    }

    private async Task<List<BsonDocument>> BackupDocumentsAsync(
        IMongoCollection<BsonDocument> source,
        IMongoCollection<BsonDocument> backup,
        FilterDefinition<BsonDocument> filter,
        CancellationToken cancellationToken)
    {
        List<BsonDocument> documents = await source.Find(filter).ToListAsync(cancellationToken);
        if (documents.Count == 0)
        {
            return documents;
        }

        List<WriteModel<BsonDocument>> writes = new List<WriteModel<BsonDocument>>(documents.Count);
        foreach (BsonDocument document in documents)
        {
            BsonDocument valuesToInsert = document.DeepClone().AsBsonDocument;
            BsonValue identifier = valuesToInsert["_id"];
            valuesToInsert.Remove("_id");
            writes.Add(new UpdateOneModel<BsonDocument>(
                Builders<BsonDocument>.Filter.Eq("_id", identifier),
                new BsonDocument("$setOnInsert", valuesToInsert))
            {
                IsUpsert = true,
            });
        }

        await backup.BulkWriteAsync(
            writes,
            new BulkWriteOptions { IsOrdered = true },
            cancellationToken);
        return documents;
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
