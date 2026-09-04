using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using Microsoft.Extensions.Logging;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class PassportAuditStore : IPassportAuditPublisher, IPassportAuditReconciler
{
    public const int MaximumReconciliationBatchSize = 50;

    private readonly IMongoCollection<PassportAuditJournalDocument> auditCollection;
    private readonly IMongoCollection<UserVisitDocument> visitCollection;
    private readonly IMongoCollection<UserRideOccurrenceDocument> occurrenceCollection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;
    private readonly ILogger<PassportAuditStore> logger;

    public PassportAuditStore(
        IMongoDatabase database,
        MongoDbSettings settings,
        ILogger<PassportAuditStore> logger)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.logger = logger;
        this.auditCollection = database.GetCollection<PassportAuditJournalDocument>(
            settings.PassportAuditEventsCollectionName);
        this.visitCollection = database.GetCollection<UserVisitDocument>(
            settings.UserVisitsCollectionName);
        this.occurrenceCollection = database.GetCollection<UserRideOccurrenceDocument>(
            settings.UserRideOccurrencesCollectionName);
        this.operationCollection =
            database.GetCollection<UserRideOccurrenceCreationOperationDocument>(
                settings.UserRideOccurrenceOperationsCollectionName);
    }

    public async Task<bool> TryPublishAsync(
        PassportAuditEvent auditEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);
        return await this.TryPublishBatchAsync(new[] { auditEvent }, cancellationToken);
    }

    public async Task<bool> TryPublishBatchAsync(
        IReadOnlyCollection<PassportAuditEvent> auditEvents,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(auditEvents);
        if (auditEvents.Count == 0)
        {
            return true;
        }

        try
        {
            PassportAuditEvent[] distinctEvents = auditEvents
                .GroupBy(static auditEvent => auditEvent.Id, StringComparer.Ordinal)
                .Select(static group => group.First())
                .ToArray();
            IReadOnlyDictionary<string, PassportAuditEvent> durableEvents =
                await this.LoadDurableEventsAsync(
                distinctEvents.Select(static auditEvent => auditEvent.Id).ToArray(),
                cancellationToken);
            PassportAuditEvent[] publishableEvents = distinctEvents
                .Where(auditEvent => durableEvents.ContainsKey(auditEvent.Id))
                .Select(auditEvent => durableEvents[auditEvent.Id])
                .ToArray();
            if (publishableEvents.Length == 0)
            {
                return true;
            }

            List<string> publishedEventIds = new List<string>(publishableEvents.Length);
            foreach (PassportAuditEvent auditEvent in publishableEvents)
            {
                PassportAuditEventDocument eventDocument = auditEvent.ToDocument();
                PassportAuditJournalDocument journalDocument =
                    new PassportAuditJournalDocument
                    {
                        Id = auditEvent.Id,
                        Event = eventDocument,
                        CreatedAt = auditEvent.OccurredAtUtc,
                        UpdatedAt = auditEvent.OccurredAtUtc,
                    };
                try
                {
                    await this.auditCollection.InsertOneAsync(
                        journalDocument,
                        cancellationToken: cancellationToken);
                }
                catch (MongoWriteException exception)
                    when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
                {
                    // Une reprise après insertion mais avant acquittement est attendue.
                }

                publishedEventIds.Add(auditEvent.Id);
            }

            await this.AcknowledgeSourceMarkersAsync(
                publishedEventIds,
                cancellationToken);
            return true;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            return false;
        }
        catch (Exception exception)
        {
            this.logger.LogError(
                exception,
                "Unable to publish a passport audit batch; unacknowledged source markers are retained.");
            return false;
        }
    }

    public async Task<int> ReconcileBatchAsync(
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        if (maximumEventCount is < 1 or > MaximumReconciliationBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(maximumEventCount));
        }

        IReadOnlyCollection<PassportAuditEvent> pending = await this.LoadPendingEventsAsync(
            maximumEventCount,
            cancellationToken);
        bool published = await this.TryPublishBatchAsync(pending, cancellationToken);
        return published ? pending.Count : 0;
    }

    private async Task<IReadOnlyCollection<PassportAuditEvent>> LoadPendingEventsAsync(
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        List<PassportAuditEventDocument> documents = new List<PassportAuditEventDocument>();
        await this.AppendVisitEventsAsync(documents, maximumEventCount, cancellationToken);
        await this.AppendOccurrenceEventsAsync(documents, maximumEventCount, cancellationToken);
        await this.AppendCompletedOperationEventsAsync(
            documents,
            maximumEventCount,
            cancellationToken);
        return documents
            .GroupBy(static document => document.EventId, StringComparer.Ordinal)
            .Select(static group => group.First().ToDomain())
            .OrderBy(static auditEvent => auditEvent.OccurredAtUtc)
            .ThenBy(static auditEvent => auditEvent.Id, StringComparer.Ordinal)
            .Take(maximumEventCount)
            .ToArray();
    }

    private async Task AppendVisitEventsAsync(
        ICollection<PassportAuditEventDocument> destination,
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        List<UserVisitDocument> sources = await this.visitCollection
            .Find(BuildPendingMarkerFilter<UserVisitDocument>())
            .Limit(maximumEventCount)
            .Project<UserVisitDocument>(Builders<UserVisitDocument>.Projection
                .Include(static document => document.PendingAuditEvents))
            .ToListAsync(cancellationToken);
        AppendUntilLimit(destination, sources.SelectMany(static source =>
            source.PendingAuditEvents ?? Enumerable.Empty<PassportAuditEventDocument>()),
            maximumEventCount);
    }

    private async Task AppendOccurrenceEventsAsync(
        ICollection<PassportAuditEventDocument> destination,
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        if (destination.Count >= maximumEventCount)
        {
            return;
        }

        List<UserRideOccurrenceDocument> sources = await this.occurrenceCollection
            .Find(BuildPendingMarkerFilter<UserRideOccurrenceDocument>())
            .Limit(maximumEventCount - destination.Count)
            .Project<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.Projection
                    .Include(static document => document.VisitId)
                    .Include(static document => document.UserId)
                    .Include(static document => document.ContentMutationFenceToken)
                    .Include(static document => document.PendingAuditEvents))
            .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, CurrentContentFence> currentFences =
            await this.LoadCurrentFencesAsync(
                sources.Select(static source => (source.UserId, source.VisitId)),
                cancellationToken);
        AppendUntilLimit(destination, sources
            .Where(source => CurrentFenceMatches(
                currentFences,
                source.UserId,
                source.VisitId,
                source.ContentMutationFenceToken))
            .SelectMany(static source =>
            source.PendingAuditEvents ?? Enumerable.Empty<PassportAuditEventDocument>()),
            maximumEventCount);
    }

    private async Task AppendCompletedOperationEventsAsync(
        ICollection<PassportAuditEventDocument> destination,
        int maximumEventCount,
        CancellationToken cancellationToken)
    {
        if (destination.Count >= maximumEventCount)
        {
            return;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            BuildPendingMarkerFilter<UserRideOccurrenceCreationOperationDocument>()
            & filters.Eq(static document => document.OperationState, "completed");
        List<UserRideOccurrenceCreationOperationDocument> sources =
            await this.operationCollection
                .Find(filter)
                .Limit(maximumEventCount - destination.Count)
                .Project<UserRideOccurrenceCreationOperationDocument>(
                    Builders<UserRideOccurrenceCreationOperationDocument>.Projection
                        .Include(static document => document.VisitId)
                        .Include(static document => document.UserId)
                        .Include(static document => document.ContentMutationFenceToken)
                        .Include(static document => document.PendingAuditEvents))
                .ToListAsync(cancellationToken);
        IReadOnlyDictionary<string, CurrentContentFence> currentFences =
            await this.LoadCurrentFencesAsync(
                sources
                    .Where(static source => !string.IsNullOrWhiteSpace(source.VisitId))
                    .Select(static source => (source.UserId, source.VisitId!)),
                cancellationToken);
        AppendUntilLimit(destination, sources
            .Where(source => !string.IsNullOrWhiteSpace(source.VisitId)
                && CurrentFenceMatches(
                    currentFences,
                    source.UserId,
                    source.VisitId,
                    source.ContentMutationFenceToken))
            .SelectMany(static source =>
            source.PendingAuditEvents ?? Enumerable.Empty<PassportAuditEventDocument>()),
            maximumEventCount);
    }

    private async Task<IReadOnlyDictionary<string, CurrentContentFence>>
        LoadCurrentFencesAsync(
        IEnumerable<(string UserId, string VisitId)> scopes,
        CancellationToken cancellationToken)
    {
        (string UserId, string VisitId)[] uniqueScopes = scopes
            .Distinct()
            .ToArray();
        if (uniqueScopes.Length == 0)
        {
            return new Dictionary<string, CurrentContentFence>(StringComparer.Ordinal);
        }

        FilterDefinition<UserVisitDocument>[] scopeFilters = uniqueScopes
            .Select(scope => UserVisitMongoDefinitions.BuildOwnedVisitFilter(
                scope.VisitId,
                scope.UserId))
            .ToArray();
        List<UserVisitDocument> visits = await this.visitCollection
            .Find(Builders<UserVisitDocument>.Filter.Or(scopeFilters))
            .Project<UserVisitDocument>(Builders<UserVisitDocument>.Projection
                .Include(static document => document.Id)
                .Include(static document => document.UserId)
                .Include(static document => document.ContentMutationFenceToken)
                .Include(static document => document.ContentMutationFenceStableToken)
                .Include(static document => document.ContentMutationFenceReady))
            .ToListAsync(cancellationToken);
        return visits.ToDictionary(
            static visit => BuildFenceScopeKey(visit.UserId, visit.Id),
            static visit => new CurrentContentFence(
                visit.ContentMutationFenceToken,
                visit.ContentMutationFenceReady,
                visit.ContentMutationFenceStableToken),
            StringComparer.Ordinal);
    }

    private static bool CurrentFenceMatches(
        IReadOnlyDictionary<string, CurrentContentFence> currentFences,
        string userId,
        string visitId,
        long? sourceFence)
    {
        return currentFences.TryGetValue(
                BuildFenceScopeKey(userId, visitId),
                out CurrentContentFence? currentFence)
            && currentFence.Matches(sourceFence);
    }

    private static string BuildFenceScopeKey(string userId, string visitId)
    {
        return string.Concat(userId, "\n", visitId);
    }

    internal static bool ContentFenceAllowsAuditDelivery(
        long? token,
        bool isReady,
        long? stableToken,
        long? sourceFence)
    {
        if (!token.HasValue)
        {
            return !sourceFence.HasValue;
        }

        if (isReady)
        {
            return sourceFence == token;
        }

        return stableToken.HasValue
            ? sourceFence >= stableToken && sourceFence <= token
            : !sourceFence.HasValue || sourceFence is >= 1 && sourceFence <= token;
    }

    private sealed record CurrentContentFence(
        long? Token,
        bool IsReady,
        long? StableToken)
    {
        public bool Matches(long? sourceFence)
        {
            return ContentFenceAllowsAuditDelivery(
                this.Token,
                this.IsReady,
                this.StableToken,
                sourceFence);
        }
    }

    private async Task<IReadOnlyDictionary<string, PassportAuditEvent>> LoadDurableEventsAsync(
        IReadOnlyCollection<string> requestedEventIds,
        CancellationToken cancellationToken)
    {
        HashSet<string> requested = requestedEventIds.ToHashSet(StringComparer.Ordinal);
        Dictionary<string, PassportAuditEvent> durable =
            new Dictionary<string, PassportAuditEvent>(StringComparer.Ordinal);
        await AppendDurableMarkerEventsAsync(
            this.visitCollection,
            requested,
            durable,
            null,
            cancellationToken);
        await AppendDurableMarkerEventsAsync(
            this.occurrenceCollection,
            requested,
            durable,
            null,
            cancellationToken);
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> operationFilters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        await AppendDurableMarkerEventsAsync(
            this.operationCollection,
            requested,
            durable,
            operationFilters.Eq(static document => document.OperationState, "completed"),
            cancellationToken);
        return durable;
    }

    private async Task AcknowledgeSourceMarkersAsync(
        IReadOnlyCollection<string> eventIds,
        CancellationToken cancellationToken)
    {
        if (eventIds.Count == 0)
        {
            return;
        }

        await PullPendingEventAsync(
            this.visitCollection,
            eventIds,
            cancellationToken);
        await PullPendingEventAsync(
            this.occurrenceCollection,
            eventIds,
            cancellationToken);
        await PullPendingEventAsync(
            this.operationCollection,
            eventIds,
            cancellationToken);
    }

    private static async Task PullPendingEventAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IReadOnlyCollection<string> eventIds,
        CancellationToken cancellationToken)
    {
        FilterDefinition<TDocument> filter = Builders<TDocument>.Filter.In(
            PassportAuditMongoDefinitions.PendingEventIdPath,
            eventIds);
        BsonDocument updateDocument = new BsonDocument(
            "$pull",
            new BsonDocument(
                "pendingAuditEvents",
                new BsonDocument(
                    "eventId",
                    new BsonDocument(
                        "$in",
                        new BsonArray(eventIds)))));
        await collection.UpdateManyAsync(
            filter,
            new BsonDocumentUpdateDefinition<TDocument>(updateDocument),
            cancellationToken: cancellationToken);
    }

    private static FilterDefinition<TDocument> BuildPendingMarkerFilter<TDocument>()
    {
        return Builders<TDocument>.Filter.Exists(
            PassportAuditMongoDefinitions.PendingEventIdPath,
            true);
    }

    private static async Task AppendDurableMarkerEventsAsync<TDocument>(
        IMongoCollection<TDocument> collection,
        IReadOnlySet<string> requestedEventIds,
        IDictionary<string, PassportAuditEvent> destination,
        FilterDefinition<TDocument>? additionalFilter,
        CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<TDocument> filters = Builders<TDocument>.Filter;
        FilterDefinition<TDocument> filter = filters.In(
            PassportAuditMongoDefinitions.PendingEventIdPath,
            requestedEventIds);
        if (additionalFilter is not null)
        {
            filter &= additionalFilter;
        }

        List<BsonDocument> sources = await collection
            .Find(filter)
            .Project(new BsonDocument("pendingAuditEvents", 1))
            .Limit(requestedEventIds.Count)
            .ToListAsync(cancellationToken);
        foreach (BsonDocument source in sources)
        {
            if (!source.TryGetValue("pendingAuditEvents", out BsonValue? value)
                || !value.IsBsonArray)
            {
                continue;
            }

            foreach (BsonValue pendingValue in value.AsBsonArray)
            {
                if (pendingValue.IsBsonDocument
                    && pendingValue.AsBsonDocument.TryGetValue("eventId", out BsonValue? id)
                    && id.IsString
                    && requestedEventIds.Contains(id.AsString)
                    && !destination.ContainsKey(id.AsString))
                {
                    PassportAuditEventDocument document =
                        BsonSerializer.Deserialize<PassportAuditEventDocument>(
                            pendingValue.AsBsonDocument);
                    destination.Add(id.AsString, document.ToDomain());
                }
            }
        }
    }

    private static void AppendUntilLimit(
        ICollection<PassportAuditEventDocument> destination,
        IEnumerable<PassportAuditEventDocument> source,
        int maximumEventCount)
    {
        foreach (PassportAuditEventDocument document in source)
        {
            if (destination.Count >= maximumEventCount)
            {
                return;
            }

            destination.Add(document);
        }
    }
}
