using System.Globalization;
using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalRelationRepository : IHistoricalRelationRepository
{
    internal const int MaximumReadBatchSize = 200;
    internal const int MaximumFairSubjectBatchSize = 20;

    private readonly IMongoCollection<HistoricalRelationDocument> collection;
    private readonly IHistoricalSourceRepository sourceRepository;
    private readonly IHistoricalSubjectPublicationStateReader subjectPublicationStateReader;
    private readonly string collectionName;

    public HistoricalRelationRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHistoricalSourceRepository sourceRepository,
        IHistoricalSubjectPublicationStateReader subjectPublicationStateReader)
    {
        this.collection = database.GetCollection<HistoricalRelationDocument>(
            settings.HistoricalRelationsCollectionName);
        this.collectionName = settings.HistoricalRelationsCollectionName;
        this.sourceRepository = sourceRepository;
        this.subjectPublicationStateReader = subjectPublicationStateReader;
    }

    public async Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalRelation relation,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(transitionReviewEvent);
        HistoricalReviewEventTargetValidator.ValidateRelationTarget(transitionReviewEvent, relation);
        HistoricalRelationDocument candidate = relation.ToDocument(transitionReviewEvent);
        HistoricalRelationDocument? existing = await this.collection.Find(document => document.Id == candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (existing is not null)
        {
            return existing.ToBsonDocument().Equals(candidate.ToBsonDocument())
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }

        HistoricalRelationDocument? predecessorDocument = await this.LoadPredecessorAsync(relation, cancellationToken);
        HistoricalRelation? predecessor = predecessorDocument?.ToDomain();
        HistoricalRelationRevisionValidator.ValidatePredecessor(relation, predecessor);
        HistoricalReviewEventTargetValidator.ValidateRelationTransition(transitionReviewEvent, relation, predecessor);
        HistoricalReviewEventChronologyValidator.Validate(
            transitionReviewEvent,
            predecessorDocument?.TransitionReviewEvent.ToDomain());
        bool sourceIsPublic = await this.subjectPublicationStateReader.IsPublicAsync(
            relation.Source,
            cancellationToken);
        bool targetIsPublic = await this.subjectPublicationStateReader.IsPublicAsync(
            relation.Target,
            cancellationToken);
        HistoricalSubjectPublicationValidator.Validate(relation, sourceIsPublic, targetIsPublic);
        IReadOnlyCollection<HistoricalSourceReference> sources = await this.sourceRepository.GetRevisionsAsync(
            relation.SourceReferences,
            cancellationToken);
        HistoricalRelationEvidenceValidator.Validate(relation, sources);
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            return HistoricalRevisionWriteDisposition.Created;
        }
        catch (MongoWriteException exception) when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            HistoricalRelationDocument? durable = await this.collection.Find(document => document.Id == candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return durable is not null && durable.ToBsonDocument().Equals(candidate.ToBsonDocument())
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }
    }

    public async Task<HistoricalRelation?> GetLatestRevisionAsync(
        Guid relationId,
        CancellationToken cancellationToken)
    {
        if (relationId == Guid.Empty)
        {
            return null;
        }

        string normalizedId = relationId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalRelationDocument? document = await this.collection.Find(item => item.RelationId == normalizedId)
            .SortByDescending(item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<HistoricalRelation>> GetLatestDecisionEligibleRevisionsTouchingSubjectsAsync(
        IReadOnlyCollection<HistoricalSubjectKey> subjects,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        if (limit < 1 || limit > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        HistoricalSubjectKey[] keys = subjects.Distinct().ToArray();
        if (keys.Length == 0)
        {
            return Array.Empty<HistoricalRelation>();
        }

        PipelineDefinition<HistoricalRelationDocument, HistoricalRelationDocument> pipeline =
            PipelineDefinition<HistoricalRelationDocument, HistoricalRelationDocument>.Create(
                BuildLatestTouchingSubjectsPipeline(keys, this.collectionName, limit));
        List<HistoricalRelationDocument> documents = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalRelation>>
        GetLatestDecisionEligibleRevisionsTouchingEachSubjectAsync(
            IReadOnlyCollection<HistoricalSubjectKey> subjects,
            int limitPerSubject,
            CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        HistoricalSubjectKey[] keys = subjects.Distinct().ToArray();
        if (keys.Length == 0)
        {
            return Array.Empty<HistoricalRelation>();
        }

        PipelineDefinition<HistoricalRelationDocument, HistoricalRelationDocument> pipeline =
            PipelineDefinition<HistoricalRelationDocument, HistoricalRelationDocument>.Create(
                BuildLatestTouchingEachSubjectPipeline(
                    keys,
                    this.collectionName,
                    limitPerSubject));
        List<HistoricalRelationDocument> documents = await this.collection.Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    internal static IReadOnlyCollection<BsonDocument> BuildLatestTouchingSubjectsPipeline(
        IReadOnlyCollection<HistoricalSubjectKey> subjects,
        string collectionName,
        int limit)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        if (subjects.Count == 0)
        {
            throw new ArgumentException("At least one historical subject is required.", nameof(subjects));
        }

        if (string.IsNullOrWhiteSpace(collectionName))
        {
            throw new ArgumentException("A MongoDB collection name is required.", nameof(collectionName));
        }

        if (limit < 1 || limit > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limit));
        }

        BsonArray endpoints = new(subjects.SelectMany(static subject => new[]
        {
            BuildEndpoint("source", subject),
            BuildEndpoint("target", subject),
        }));
        return new BsonDocument[]
        {
            new BsonDocument("$match", new BsonDocument("$or", endpoints)),
            new BsonDocument("$group", new BsonDocument("_id", "$relationId")),
            new BsonDocument("$lookup", new BsonDocument
            {
                ["from"] = collectionName,
                ["let"] = new BsonDocument("candidateId", "$_id"),
                ["pipeline"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument("$expr", new BsonDocument("$eq", new BsonArray { "$relationId", "$$candidateId" }))),
                    new BsonDocument("$sort", new BsonDocument("revision", -1)),
                    new BsonDocument("$limit", 1),
                },
                ["as"] = "latest",
            }),
            new BsonDocument("$unwind", "$latest"),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latest")),
            new BsonDocument("$match", new BsonDocument("$or", endpoints)),
            new BsonDocument("$match", new BsonDocument
            {
                ["publicationState"] = HistoricalPublicationState.Published.ToString(),
                ["state"] = new BsonDocument("$in", new BsonArray
                {
                    HistoricalFactState.Verified.ToString(),
                    HistoricalFactState.Probable.ToString(),
                    HistoricalFactState.Disputed.ToString(),
                }),
            }),
            new BsonDocument("$sort", new BsonDocument { ["period.start.year"] = 1, ["relationId"] = 1 }),
            new BsonDocument("$limit", limit),
        };
    }

    internal static IReadOnlyCollection<BsonDocument> BuildLatestTouchingEachSubjectPipeline(
        IReadOnlyCollection<HistoricalSubjectKey> subjects,
        string collectionName,
        int limitPerSubject)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        HistoricalSubjectKey[] keys = subjects.Distinct().ToArray();
        if (keys.Length == 0 || keys.Length > MaximumFairSubjectBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(subjects));
        }

        if (limitPerSubject < 1 || limitPerSubject > MaximumReadBatchSize)
        {
            throw new ArgumentOutOfRangeException(nameof(limitPerSubject));
        }

        BsonDocument facets = new();
        BsonArray facetReferences = new();
        for (int index = 0; index < keys.Length; index++)
        {
            string facetName = $"subject_{index}";
            facets[facetName] = new BsonArray(BuildLatestTouchingSubjectsPipeline(
                new[] { keys[index] },
                collectionName,
                limitPerSubject));
            facetReferences.Add($"${facetName}");
        }

        return new BsonDocument[]
        {
            new BsonDocument("$facet", facets),
            new BsonDocument("$project", new BsonDocument(
                "relations",
                new BsonDocument("$concatArrays", facetReferences))),
            new BsonDocument("$unwind", "$relations"),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$relations")),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$relationId",
                ["relation"] = new BsonDocument("$first", "$$ROOT"),
            }),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$relation")),
            new BsonDocument("$sort", new BsonDocument
            {
                ["period.start.year"] = 1,
                ["relationId"] = 1,
            }),
        };
    }

    private static BsonDocument BuildEndpoint(string endpoint, HistoricalSubjectKey subject)
    {
        return new BsonDocument
        {
            [$"{endpoint}.type"] = subject.Type.ToString(),
            [$"{endpoint}.id"] = subject.Id,
            [$"{endpoint}.contextParkId"] = subject.ContextParkId is null
                ? BsonNull.Value
                : subject.ContextParkId,
        };
    }

    private async Task<HistoricalRelationDocument?> LoadPredecessorAsync(
        HistoricalRelation relation,
        CancellationToken cancellationToken)
    {
        if (!relation.SupersedesRevision.HasValue)
        {
            return null;
        }

        string relationId = relation.Id.ToString("N", CultureInfo.InvariantCulture);
        return await this.collection.Find(document => document.RelationId == relationId
                && document.Revision == relation.SupersedesRevision.Value)
            .FirstOrDefaultAsync(cancellationToken);
    }
}
