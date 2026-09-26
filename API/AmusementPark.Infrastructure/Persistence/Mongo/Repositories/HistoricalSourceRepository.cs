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

    public HistoricalSourceRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
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

        if (normalizedSourceIds.Length > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceIds),
                $"A historical source batch cannot exceed {MaximumBatchSize} identifiers.");
        }

        PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument> pipeline =
            PipelineDefinition<HistoricalSourceDocument, HistoricalSourceDocument>.Create(
                BuildLatestRevisionsPipeline(normalizedSourceIds));
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
        HistoricalSourceRevisionReference[] normalizedReferences = sourceReferences
            .DistinctBy(static sourceReference => (sourceReference.SourceId, sourceReference.Revision))
            .OrderBy(static sourceReference => sourceReference.SourceId)
            .ThenBy(static sourceReference => sourceReference.Revision)
            .ToArray();
        if (normalizedReferences.Length == 0)
        {
            return Array.Empty<HistoricalSourceReference>();
        }

        FilterDefinitionBuilder<HistoricalSourceDocument> builder =
            Builders<HistoricalSourceDocument>.Filter;
        List<HistoricalSourceDocument> documents = new List<HistoricalSourceDocument>(
            normalizedReferences.Length);
        foreach (HistoricalSourceRevisionReference[] batch in normalizedReferences.Chunk(MaximumBatchSize))
        {
            FilterDefinition<HistoricalSourceDocument>[] revisionFilters = batch
                .Select(sourceReference =>
                    builder.Eq(
                        document => document.SourceId,
                        sourceReference.SourceId.ToString("N", CultureInfo.InvariantCulture))
                    & builder.Eq(document => document.Revision, sourceReference.Revision))
                .ToArray();
            List<HistoricalSourceDocument> batchDocuments = await this.collection
                .Find(builder.Or(revisionFilters))
                .SortBy(document => document.SourceId)
                .ThenBy(document => document.Revision)
                .Limit(batch.Length)
                .ToListAsync(cancellationToken);
            documents.AddRange(batchDocuments);
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
}
