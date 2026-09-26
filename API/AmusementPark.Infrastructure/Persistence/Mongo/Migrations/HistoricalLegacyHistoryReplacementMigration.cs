using System.Globalization;
using System.Security.Cryptography;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Migrations;

/// <summary>
/// Remplace une seule fois la collection narrative historique par le registre canonique.
/// L'ancienne collection reste une sauvegarde de retour arrière et n'est plus lue après la bascule.
/// </summary>
public sealed class HistoricalLegacyHistoryReplacementMigration
{
    internal const string MigrationId = "hist-04-history-events-v1";
    internal const string MigrationActor = "system:hist-04-migration";
    internal const string MethodologyVersion = "hist-v1-legacy";
    internal static readonly TimeSpan LeaseDuration = TimeSpan.FromMinutes(2);

    private readonly IMongoCollection<BsonDocument> legacyCollection;
    private readonly IMongoCollection<BsonDocument> backupCollection;
    private readonly IMongoCollection<BsonDocument> narrativeCollection;
    private readonly IMongoCollection<HistoricalLegacyMigrationStateDocument> stateCollection;
    private readonly IMongoCollection<HistoricalLegacyMigrationAnomalyDocument> anomalyCollection;
    private readonly IMongoCollection<HistoricalFactDocument> factCollection;
    private readonly IMongoCollection<HistoricalSourceDocument> sourceCollection;
    private readonly HistoricalLegacyFactConverter factConverter;
    private readonly HistoricalLegacySubjectResolver subjectResolver;
    private readonly TimeProvider timeProvider;
    private readonly ILogger<HistoricalLegacyHistoryReplacementMigration> logger;

    public HistoricalLegacyHistoryReplacementMigration(
        IMongoDatabase database,
        MongoDbSettings settings,
        HistoricalLegacyFactConverter factConverter,
        HistoricalLegacySubjectResolver subjectResolver,
        ILogger<HistoricalLegacyHistoryReplacementMigration> logger)
        : this(database, settings, factConverter, subjectResolver, TimeProvider.System, logger)
    {
    }

    internal HistoricalLegacyHistoryReplacementMigration(
        IMongoDatabase database,
        MongoDbSettings settings,
        HistoricalLegacyFactConverter factConverter,
        HistoricalLegacySubjectResolver subjectResolver,
        TimeProvider timeProvider,
        ILogger<HistoricalLegacyHistoryReplacementMigration> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.factConverter = factConverter ?? throw new ArgumentNullException(nameof(factConverter));
        this.subjectResolver = subjectResolver ?? throw new ArgumentNullException(nameof(subjectResolver));
        this.timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        this.logger = logger ?? throw new ArgumentNullException(nameof(logger));
        this.legacyCollection = database.GetCollection<BsonDocument>(settings.HistoryEventsCollectionName);
        this.backupCollection = database.GetCollection<BsonDocument>(settings.HistoricalEventsBackupCollectionName);
        this.narrativeCollection = database.GetCollection<BsonDocument>(settings.HistoricalNarrativesCollectionName);
        this.stateCollection = database.GetCollection<HistoricalLegacyMigrationStateDocument>(
            settings.HistoricalMigrationsCollectionName);
        this.anomalyCollection = database.GetCollection<HistoricalLegacyMigrationAnomalyDocument>(
            settings.HistoricalMigrationAnomaliesCollectionName);
        this.factCollection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
        this.sourceCollection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await this.EnsureStateExistsAsync(cancellationToken);
        string leaseOwner = Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture);
        if (!await this.AcquireLeaseAsync(leaseOwner, cancellationToken))
        {
            return;
        }

        try
        {
            await this.ResetIncompleteMigrationOutputAsync(cancellationToken);
            (long sourceCount, string sourceDigest) = await this.CalculateSourceSnapshotAsync(cancellationToken);
            (long subjectCount, string subjectDigest) = await this.CalculateSubjectSnapshotAsync(
                true,
                cancellationToken);
            long factCount = 0;
            long sourceReferenceCount = 0;
            long blockedCount = 0;
            long warningCount = 0;

            using IAsyncCursor<BsonDocument> cursor = await this.legacyCollection
                .Find(Builders<BsonDocument>.Filter.Empty)
                .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
                .ToCursorAsync(cancellationToken);
            while (await cursor.MoveNextAsync(cancellationToken))
            {
                foreach (BsonDocument legacyDocument in cursor.Current)
                {
                    await this.RefreshLeaseAsync(leaseOwner, cancellationToken);
                    HistoricalLegacyMigrationResult result = await this.MigrateDocumentAsync(
                        legacyDocument,
                        cancellationToken);
                    factCount += result.IsBlocked ? 0 : 1;
                    sourceReferenceCount += result.SourceReferenceCount;
                    blockedCount += result.IsBlocked ? 1 : 0;
                    warningCount += result.Warnings.Count;
                }
            }

            (long finalSourceCount, string finalSourceDigest) =
                await this.CalculateSourceSnapshotAsync(cancellationToken);
            if (sourceCount != finalSourceCount
                || !string.Equals(sourceDigest, finalSourceDigest, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Legacy history changed while its replacement migration was running.");
            }

            (long finalSubjectCount, string finalSubjectDigest) = await this.CalculateSubjectSnapshotAsync(
                false,
                cancellationToken);
            EnsureSubjectSnapshotUnchanged(
                subjectCount,
                subjectDigest,
                finalSubjectCount,
                finalSubjectDigest);

            long backupCount = await this.backupCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Empty,
                cancellationToken: cancellationToken);
            long narrativeCount = await this.narrativeCollection.CountDocumentsAsync(
                Builders<BsonDocument>.Filter.Eq("migrationVersion", MigrationId),
                cancellationToken: cancellationToken);
            if (backupCount != sourceCount
                || narrativeCount != sourceCount
                || factCount + blockedCount != sourceCount)
            {
                throw new InvalidOperationException(
                    "Legacy history replacement counts do not match the source snapshot.");
            }

            await this.CompleteAsync(
                leaseOwner,
                sourceCount,
                sourceDigest,
                subjectCount,
                subjectDigest,
                backupCount,
                narrativeCount,
                factCount,
                sourceReferenceCount,
                blockedCount,
                warningCount,
                cancellationToken);
            this.logger.LogInformation(
                "Completed historical replacement: {NarrativeCount} narratives, {FactCount} facts, {BlockedCount} blocked.",
                narrativeCount,
                factCount,
                blockedCount);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await this.RecordFailureAsync(leaseOwner, exception, cancellationToken);
            throw;
        }
    }

    private async Task<HistoricalLegacyMigrationResult> MigrateDocumentAsync(
        BsonDocument legacyDocument,
        CancellationToken cancellationToken)
    {
        BsonValue legacyIdentifier = legacyDocument.GetValue("_id");
        string legacyId = legacyIdentifier.ToString()
            ?? throw new InvalidOperationException("A legacy history event requires an identifier.");
        await this.backupCollection.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", legacyIdentifier),
            legacyDocument.DeepClone().AsBsonDocument,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);

        List<string> warnings = new List<string>();
        string? canonicalFactId = null;
        int sourceReferenceCount = 0;
        bool blocked = false;
        try
        {
            HistoryEventDocument historyEvent = BsonSerializer.Deserialize<HistoryEventDocument>(legacyDocument);
            HistoricalLegacyMigrationResult conversion = await this.factConverter.ConvertAndPersistAsync(
                historyEvent,
                warnings,
                cancellationToken);
            canonicalFactId = conversion.CanonicalFactId;
            sourceReferenceCount = conversion.SourceReferenceCount;
            blocked = conversion.IsBlocked;
        }
        catch (Exception exception) when (exception is HistoricalPersistenceValidationException
            or HistoricalTemporalValidationException
            or BsonSerializationException
            or FormatException)
        {
            blocked = true;
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.ConversionFailed);
            this.logger.LogWarning(
                exception,
                "Legacy history event {LegacyEventId} requires manual conversion.",
                legacyId);
        }

        AddAssociationWarning(legacyDocument, warnings);
        warnings = warnings.Distinct(StringComparer.Ordinal).OrderBy(static value => value).ToList();
        BsonDocument narrative = legacyDocument.DeepClone().AsBsonDocument;
        narrative["canonicalizationState"] = blocked
            ? HistoricalNarrativeCanonicalizationState.Blocked.ToString()
            : HistoricalNarrativeCanonicalizationState.Migrated.ToString();
        narrative["migrationVersion"] = MigrationId;
        narrative["migrationWarnings"] = new BsonArray(warnings);
        if (canonicalFactId is null)
        {
            narrative.Remove("canonicalFactId");
        }
        else
        {
            narrative["canonicalFactId"] = canonicalFactId;
        }

        await this.narrativeCollection.ReplaceOneAsync(
            Builders<BsonDocument>.Filter.Eq("_id", legacyIdentifier),
            narrative,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
        await this.UpsertAnomalyAsync(legacyId, warnings, cancellationToken);
        return new HistoricalLegacyMigrationResult(
            canonicalFactId,
            blocked,
            sourceReferenceCount,
            warnings);
    }

    private async Task ResetIncompleteMigrationOutputAsync(CancellationToken cancellationToken)
    {
        await this.factCollection.DeleteManyAsync(
            fact => fact.RevisionOrigin == HistoricalRevisionOrigin.LegacyMigration
                && fact.PublicationMethodologyVersion == MethodologyVersion,
            cancellationToken);
        await this.sourceCollection.DeleteManyAsync(
            source => source.RevisionOrigin == HistoricalRevisionOrigin.LegacyMigration
                && source.TransitionReviewEvent.ActorUserId == MigrationActor,
            cancellationToken);
        await this.narrativeCollection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Eq("migrationVersion", MigrationId),
            cancellationToken);
        await this.anomalyCollection.DeleteManyAsync(
            anomaly => anomaly.MigrationId == MigrationId,
            cancellationToken);
        await this.backupCollection.DeleteManyAsync(
            Builders<BsonDocument>.Filter.Empty,
            cancellationToken);
    }

    private async Task<(long Count, string Digest)> CalculateSourceSnapshotAsync(
        CancellationToken cancellationToken)
    {
        long count = 0;
        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        using IAsyncCursor<BsonDocument> cursor = await this.legacyCollection
            .Find(Builders<BsonDocument>.Filter.Empty)
            .Sort(Builders<BsonDocument>.Sort.Ascending("_id"))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument document in cursor.Current)
            {
                hash.AppendData(document.ToBson());
                count++;
            }
        }

        return (count, Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    private async Task<(long Count, string Digest)> CalculateSubjectSnapshotAsync(
        bool useCachedResolution,
        CancellationToken cancellationToken)
    {
        HashSet<(string EntityType, string OwnerId)> subjectKeys = new();
        using IAsyncCursor<BsonDocument> cursor = await this.legacyCollection
            .Find(Builders<BsonDocument>.Filter.Empty)
            .Project(Builders<BsonDocument>.Projection.Include("entityType").Include("ownerId"))
            .ToCursorAsync(cancellationToken);
        while (await cursor.MoveNextAsync(cancellationToken))
        {
            foreach (BsonDocument document in cursor.Current)
            {
                string entityType = document.GetValue("entityType", BsonNull.Value).ToString()
                    ?? string.Empty;
                string ownerId = document.GetValue("ownerId", BsonNull.Value).ToString()
                    ?? string.Empty;
                subjectKeys.Add((entityType, ownerId));
            }
        }

        using IncrementalHash hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        foreach ((string entityType, string ownerId) in subjectKeys
                     .OrderBy(static key => key.EntityType, StringComparer.Ordinal)
                     .ThenBy(static key => key.OwnerId, StringComparer.Ordinal))
        {
            BsonDocument snapshot = new BsonDocument
            {
                { "entityType", entityType },
                { "ownerId", ownerId },
            };
            if (Enum.TryParse(entityType, true, out HistoryEntityType parsedEntityType)
                && Enum.IsDefined(parsedEntityType)
                && !string.IsNullOrWhiteSpace(ownerId))
            {
                HistoryEventDocument subjectEvent = new HistoryEventDocument
                {
                    EntityType = parsedEntityType,
                    OwnerId = ownerId,
                };
                HistoricalLegacySubjectResolution resolution = useCachedResolution
                    ? await this.subjectResolver.ResolveAsync(subjectEvent, cancellationToken)
                    : await this.subjectResolver.ResolveCurrentAsync(subjectEvent, cancellationToken);
                snapshot.Add("subjectType", resolution.SubjectType.ToString());
                snapshot.Add("historicalLabel", resolution.HistoricalLabel);
                snapshot.Add("publicationPolicy", resolution.PublicationPolicy.ToString());
                snapshot.Add(
                    "anomalyCode",
                    resolution.AnomalyCode is null ? BsonNull.Value : resolution.AnomalyCode);
            }
            else
            {
                snapshot.Add("resolution", "invalid");
            }

            hash.AppendData(snapshot.ToBson());
        }

        return (
            subjectKeys.Count,
            Convert.ToHexString(hash.GetHashAndReset()).ToLowerInvariant());
    }

    internal static void EnsureSubjectSnapshotUnchanged(
        long expectedCount,
        string expectedDigest,
        long actualCount,
        string actualDigest)
    {
        if (expectedCount != actualCount
            || !string.Equals(expectedDigest, actualDigest, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "A historical subject changed while its replacement migration was running.");
        }
    }

    private async Task EnsureStateExistsAsync(CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        await this.stateCollection.UpdateOneAsync(
            state => state.Id == MigrationId,
            Builders<HistoricalLegacyMigrationStateDocument>.Update
                .SetOnInsert(static state => state.Id, MigrationId)
                .SetOnInsert(static state => state.StartedAtUtc, nowUtc)
                .SetOnInsert(static state => state.UpdatedAtUtc, nowUtc),
            new UpdateOptions { IsUpsert = true },
            cancellationToken);
    }

    private async Task<bool> AcquireLeaseAsync(string leaseOwner, CancellationToken cancellationToken)
    {
        while (true)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            FilterDefinition<HistoricalLegacyMigrationStateDocument> claimable =
                Builders<HistoricalLegacyMigrationStateDocument>.Filter.Eq(static state => state.Id, MigrationId)
                & Builders<HistoricalLegacyMigrationStateDocument>.Filter.Eq(static state => state.CompletedAtUtc, null)
                & (Builders<HistoricalLegacyMigrationStateDocument>.Filter.Eq(static state => state.LeaseOwner, null)
                    | Builders<HistoricalLegacyMigrationStateDocument>.Filter.Lte(
                        static state => state.LeaseExpiresAtUtc,
                        nowUtc));
            HistoricalLegacyMigrationStateDocument? state = await this.stateCollection.FindOneAndUpdateAsync(
                claimable,
                Builders<HistoricalLegacyMigrationStateDocument>.Update
                    .Set(static item => item.LeaseOwner, leaseOwner)
                    .Set(static item => item.LeaseExpiresAtUtc, nowUtc.Add(LeaseDuration))
                    .Set(static item => item.UpdatedAtUtc, nowUtc)
                    .Set(static item => item.LastError, null),
                new FindOneAndUpdateOptions<HistoricalLegacyMigrationStateDocument>
                {
                    ReturnDocument = ReturnDocument.After,
                },
                cancellationToken);
            if (state is not null)
            {
                return true;
            }

            state = await this.stateCollection.Find(item => item.Id == MigrationId)
                .FirstOrDefaultAsync(cancellationToken);
            if (state?.CompletedAtUtc is not null)
            {
                return false;
            }

            await Task.Delay(TimeSpan.FromSeconds(2), this.timeProvider, cancellationToken);
        }
    }

    private async Task RefreshLeaseAsync(string leaseOwner, CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UpdateResult result = await this.stateCollection.UpdateOneAsync(
            state => state.Id == MigrationId
                && state.CompletedAtUtc == null
                && state.LeaseOwner == leaseOwner,
            Builders<HistoricalLegacyMigrationStateDocument>.Update
                .Set(static state => state.LeaseExpiresAtUtc, nowUtc.Add(LeaseDuration))
                .Set(static state => state.UpdatedAtUtc, nowUtc),
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new InvalidOperationException("The historical replacement migration lease was lost.");
        }
    }

    private async Task CompleteAsync(
        string leaseOwner,
        long sourceCount,
        string sourceDigest,
        long subjectCount,
        string subjectDigest,
        long backupCount,
        long narrativeCount,
        long factCount,
        long sourceReferenceCount,
        long blockedCount,
        long warningCount,
        CancellationToken cancellationToken)
    {
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        UpdateResult result = await this.stateCollection.UpdateOneAsync(
            state => state.Id == MigrationId
                && state.CompletedAtUtc == null
                && state.LeaseOwner == leaseOwner,
            Builders<HistoricalLegacyMigrationStateDocument>.Update
                .Set(static state => state.SourceCount, sourceCount)
                .Set(static state => state.SourceDigest, sourceDigest)
                .Set(static state => state.SubjectCount, subjectCount)
                .Set(static state => state.SubjectDigest, subjectDigest)
                .Set(static state => state.BackupCount, backupCount)
                .Set(static state => state.NarrativeCount, narrativeCount)
                .Set(static state => state.FactCount, factCount)
                .Set(static state => state.SourceReferenceCount, sourceReferenceCount)
                .Set(static state => state.BlockedCount, blockedCount)
                .Set(static state => state.WarningCount, warningCount)
                .Set(static state => state.CompletedAtUtc, nowUtc)
                .Set(static state => state.LeaseOwner, null)
                .Set(static state => state.LeaseExpiresAtUtc, null)
                .Set(static state => state.LastError, null)
                .Set(static state => state.UpdatedAtUtc, nowUtc),
            cancellationToken: cancellationToken);
        if (result.MatchedCount != 1)
        {
            throw new InvalidOperationException("The historical replacement cutover marker was not committed.");
        }
    }

    private async Task RecordFailureAsync(
        string leaseOwner,
        Exception exception,
        CancellationToken cancellationToken)
    {
        this.logger.LogError(exception, "Historical replacement failed before cutover.");
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        string error = exception.GetType().Name;
        try
        {
            await this.stateCollection.UpdateOneAsync(
                state => state.Id == MigrationId
                    && state.CompletedAtUtc == null
                    && state.LeaseOwner == leaseOwner,
                Builders<HistoricalLegacyMigrationStateDocument>.Update
                    .Set(static state => state.LeaseOwner, null)
                    .Set(static state => state.LeaseExpiresAtUtc, null)
                    .Set(static state => state.LastError, error)
                    .Set(static state => state.UpdatedAtUtc, nowUtc),
                cancellationToken: cancellationToken);
        }
        catch (Exception recordingException) when (recordingException is not OperationCanceledException)
        {
            this.logger.LogError(recordingException, "Could not record the historical replacement failure.");
        }
    }

    private async Task UpsertAnomalyAsync(
        string legacyId,
        IReadOnlyCollection<string> warnings,
        CancellationToken cancellationToken)
    {
        string anomalyId = HistoricalLegacyMigrationIdentity.CreateAnomalyId(MigrationId, legacyId);
        if (warnings.Count == 0)
        {
            await this.anomalyCollection.DeleteOneAsync(item => item.Id == anomalyId, cancellationToken);
            return;
        }

        HistoricalLegacyMigrationAnomalyDocument anomaly = new HistoricalLegacyMigrationAnomalyDocument
        {
            Id = anomalyId,
            MigrationId = MigrationId,
            LegacyEventId = legacyId,
            Codes = warnings.ToList(),
            RecordedAtUtc = this.timeProvider.GetUtcNow().UtcDateTime,
        };
        await this.anomalyCollection.ReplaceOneAsync(
            item => item.Id == anomalyId,
            anomaly,
            new ReplaceOptions { IsUpsert = true },
            cancellationToken);
    }

    private static void AddAssociationWarning(BsonDocument legacyDocument, List<string> warnings)
    {
        bool hasParkRelations = legacyDocument.TryGetValue("relatedParkIds", out BsonValue? relatedParks)
            && relatedParks.IsBsonArray
            && relatedParks.AsBsonArray.Count > 0;
        bool hasParkItemRelations = legacyDocument.TryGetValue(
                "relatedParkItemIds",
                out BsonValue? relatedItems)
            && relatedItems.IsBsonArray
            && relatedItems.AsBsonArray.Count > 0;
        if (hasParkRelations || hasParkItemRelations)
        {
            warnings.Add(HistoricalLegacyMigrationAnomalyCodes.UnconvertedAssociations);
        }
    }
}
