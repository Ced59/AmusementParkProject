using System.Globalization;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalSourceRepository : IHistoricalSourceRepository
{
    internal const int MaximumBatchSize = 500;

    private readonly IMongoCollection<HistoricalSourceDocument> collection;
    private readonly IHistoricalParkRolloutGateCache rolloutGateCache;

    public HistoricalSourceRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHistoricalParkRolloutGateCache rolloutGateCache)
    {
        this.collection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
        this.rolloutGateCache = rolloutGateCache
            ?? throw new ArgumentNullException(nameof(rolloutGateCache));
    }

    public async Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalSourceReference source,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(transitionReviewEvent);
        HistoricalReviewEventTargetValidator.ValidateSourceTarget(transitionReviewEvent, source);
        HistoricalSourceDocument candidate = source.ToDocument(transitionReviewEvent);
        HistoricalSourceDocument? durableRevision = await this.collection
            .Find(document => document.Id == candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (durableRevision is not null)
        {
            return DocumentsMatch(durableRevision, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }

        HistoricalSourceDocument? predecessorDocument =
            await this.LoadPredecessorDocumentAsync(source, cancellationToken);
        HistoricalSourceReference? predecessor = predecessorDocument?.ToDomain();
        HistoricalSourceRevisionValidator.ValidatePredecessor(source, predecessor);
        HistoricalReviewEventTargetValidator.ValidateSourceTransition(
            transitionReviewEvent,
            source,
            predecessor);
        HistoricalReviewEventChronologyValidator.Validate(
            transitionReviewEvent,
            predecessorDocument?.TransitionReviewEvent.ToDomain());
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            this.rolloutGateCache.Invalidate();
            return HistoricalRevisionWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            HistoricalSourceDocument? existing = await this.collection
                .Find(document => document.Id == candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return DocumentsMatch(existing, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }
    }

    public async Task<HistoricalSourceReference?> GetRevisionAsync(
        Guid sourceId,
        int revision,
        CancellationToken cancellationToken)
    {
        if (sourceId == Guid.Empty || revision <= 0)
        {
            return null;
        }

        string normalizedSourceId = sourceId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalSourceDocument? document = await this.collection
            .Find(item => item.SourceId == normalizedSourceId && item.Revision == revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<HistoricalSourceReference?> GetLatestRevisionAsync(
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        if (sourceId == Guid.Empty)
        {
            return null;
        }

        string normalizedSourceId = sourceId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalSourceDocument? document = await this.collection
            .Find(item => item.SourceId == normalizedSourceId)
            .SortByDescending(item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<bool> WasLatestRevisionTransitionRecordedByAsync(
        Guid sourceId,
        HistoricalReviewEventType eventType,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        string normalizedActorUserId = actorUserId?.Trim() ?? string.Empty;
        if (sourceId == Guid.Empty || normalizedActorUserId.Length == 0)
        {
            return false;
        }

        string normalizedSourceId = sourceId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalReviewEventDocument? reviewEvent = await this.collection
            .Find(item => item.SourceId == normalizedSourceId)
            .SortByDescending(item => item.Revision)
            .Project(item => item.TransitionReviewEvent)
            .FirstOrDefaultAsync(cancellationToken);
        return reviewEvent is not null
            && reviewEvent.EventType == eventType
            && string.Equals(
                reviewEvent.ActorUserId,
                normalizedActorUserId,
                StringComparison.Ordinal);
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetLatestRevisionsAsync(
        IReadOnlyCollection<Guid> sourceIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceIds);
        string[] normalizedSourceIds = sourceIds
            .Where(static sourceId => sourceId != Guid.Empty)
            .Distinct()
            .Select(static sourceId => sourceId.ToString("N", CultureInfo.InvariantCulture))
            .ToArray();
        if (normalizedSourceIds.Length == 0)
        {
            return Array.Empty<HistoricalSourceReference>();
        }

        List<HistoricalSourceDocument> documents = new List<HistoricalSourceDocument>(
            normalizedSourceIds.Length);
        foreach (string[] batch in normalizedSourceIds.Chunk(MaximumBatchSize))
        {
            PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument> pipeline =
                PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument>.Create(
                    BuildLatestRevisionsPipeline(batch));
            documents.AddRange(await this.collection
                .Aggregate(pipeline)
                .ToListAsync(cancellationToken));
        }

        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetRecentLatestRevisionsAsync(
        int limit,
        CancellationToken cancellationToken)
    {
        int boundedLimit = Math.Clamp(limit, 1, 200);
        PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument> pipeline =
            PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument>.Create(
                BuildRecentLatestRevisionsPipeline(boundedLimit));
        List<HistoricalSourceDocument> documents = await this.collection
            .Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetRevisionsAsync(
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceReferences);
        HistoricalSourceRevisionKey[] revisionKeys = sourceReferences
            .Select(static reference => new HistoricalSourceRevisionKey(
                reference.SourceId,
                reference.Revision))
            .ToArray();
        return await this.GetRevisionsAsync(revisionKeys, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetRevisionsAsync(
        IReadOnlyCollection<HistoricalRelationSourceRevisionReference> sourceReferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceReferences);
        HistoricalSourceRevisionKey[] revisionKeys = sourceReferences
            .Select(static reference => new HistoricalSourceRevisionKey(
                reference.SourceId,
                reference.Revision))
            .ToArray();
        return await this.GetRevisionsAsync(revisionKeys, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetRevisionsAsync(
        IReadOnlyCollection<HistoricalSourceRevisionKey> sourceReferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceReferences);
        HistoricalSourceRevisionKey[] normalizedReferences = sourceReferences
            .Distinct()
            .OrderBy(static reference => reference.SourceId)
            .ThenBy(static reference => reference.Revision)
            .ToArray();
        if (normalizedReferences.Length == 0)
        {
            return Array.Empty<HistoricalSourceReference>();
        }

        FilterDefinitionBuilder<HistoricalSourceDocument> builder = Builders<HistoricalSourceDocument>.Filter;
        List<HistoricalSourceDocument> documents = new(normalizedReferences.Length);
        foreach (HistoricalSourceRevisionKey[] batch in normalizedReferences.Chunk(MaximumBatchSize))
        {
            FilterDefinition<HistoricalSourceDocument>[] filters = batch.Select(reference =>
                builder.Eq(
                    document => document.SourceId,
                    reference.SourceId.ToString("N", CultureInfo.InvariantCulture))
                & builder.Eq(document => document.Revision, reference.Revision)).ToArray();
            documents.AddRange(await this.collection.Find(builder.Or(filters))
                .Limit(batch.Length)
                .ToListAsync(cancellationToken));
        }

        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    private static bool DocumentsMatch(
        HistoricalSourceDocument? existing,
        HistoricalSourceDocument candidate)
    {
        return existing is not null
            && existing.ToBsonDocument().Equals(candidate.ToBsonDocument());
    }

    private async Task<HistoricalSourceDocument?> LoadPredecessorDocumentAsync(
        HistoricalSourceReference source,
        CancellationToken cancellationToken)
    {
        if (source.Revision == 1)
        {
            return null;
        }

        string normalizedSourceId = source.Id.ToString("N", CultureInfo.InvariantCulture);
        int predecessorRevision = source.Revision - 1;
        return await this.collection
            .Find(document => document.SourceId == normalizedSourceId
                && document.Revision == predecessorRevision)
            .FirstOrDefaultAsync(cancellationToken);
    }

    internal static IReadOnlyCollection<BsonDocument> BuildLatestRevisionsPipeline(
        IReadOnlyCollection<string> normalizedSourceIds)
    {
        ArgumentNullException.ThrowIfNull(normalizedSourceIds);
        return new BsonDocument[]
        {
            new BsonDocument(
                "$match",
                new BsonDocument(
                    "sourceId",
                    new BsonDocument("$in", new BsonArray(normalizedSourceIds)))),
            new BsonDocument("$sort", new BsonDocument
            {
                ["sourceId"] = 1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$sourceId",
                ["document"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument(
                "$replaceRoot",
                new BsonDocument("newRoot", "$document")),
            new BsonDocument("$sort", new BsonDocument("sourceId", 1)),
        };
    }

    internal static IReadOnlyCollection<BsonDocument> BuildRecentLatestRevisionsPipeline(int limit)
    {
        if (limit < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$sort", new BsonDocument
            {
                ["sourceId"] = 1,
                ["revision"] = -1,
            }),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$sourceId",
                ["document"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument(
                "$replaceRoot",
                new BsonDocument("newRoot", "$document")),
            new BsonDocument("$sort", new BsonDocument("createdAt", -1)),
            new BsonDocument("$limit", limit),
        };
    }
}
