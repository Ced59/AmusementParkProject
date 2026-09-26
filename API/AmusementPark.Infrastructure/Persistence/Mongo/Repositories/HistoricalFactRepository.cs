using System.Globalization;
using AmusementPark.Application.Common.Results;
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

public sealed class HistoricalFactRepository : IHistoricalFactRepository
{
    private readonly IMongoCollection<HistoricalFactDocument> collection;
    private readonly IMongoCollection<HistoricalSourceDocument> sourceCollection;
    private readonly IHistoricalSubjectPublicationStateReader subjectPublicationStateReader;
    private readonly string collectionName;

    public HistoricalFactRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHistoricalSubjectPublicationStateReader subjectPublicationStateReader)
    {
        this.collection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
        this.collectionName = settings.HistoricalFactsCollectionName;
        this.sourceCollection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
        this.subjectPublicationStateReader = subjectPublicationStateReader
            ?? throw new ArgumentNullException(nameof(subjectPublicationStateReader));
    }

    public async Task<HistoricalRevisionWriteDisposition> AppendRevisionAsync(
        HistoricalFact fact,
        HistoricalReviewEvent transitionReviewEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(fact);
        ArgumentNullException.ThrowIfNull(transitionReviewEvent);
        HistoricalReviewEventTargetValidator.ValidateFactTarget(transitionReviewEvent, fact);
        HistoricalFactDocument candidate = fact.ToDocument(transitionReviewEvent);
        HistoricalFactDocument? durableRevision = await this.collection
            .Find(document => document.Id == candidate.Id)
            .FirstOrDefaultAsync(cancellationToken);
        if (durableRevision is not null)
        {
            return DocumentsMatch(durableRevision, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }

        HistoricalFactDocument? predecessorDocument =
            await this.LoadPredecessorDocumentAsync(fact, cancellationToken);
        HistoricalFact? predecessor = predecessorDocument?.ToDomain();
        HistoricalFactRevisionValidator.ValidatePredecessor(fact, predecessor);
        HistoricalReviewEventTargetValidator.ValidateFactTransition(
            transitionReviewEvent,
            fact,
            predecessor);
        HistoricalReviewEventChronologyValidator.Validate(
            transitionReviewEvent,
            predecessorDocument?.TransitionReviewEvent.ToDomain());
        bool requiresCurrentSubjectResolution = fact.Subject.PublicationPolicy
                == HistoricalSubjectPublicationPolicy.FollowCurrentSubject
            && fact.PublicationState is HistoricalPublicationState.Published
                or HistoricalPublicationState.LegacyPublishedPendingReview;
        bool currentSubjectIsPublic = !requiresCurrentSubjectResolution
            || await this.subjectPublicationStateReader.IsPublicAsync(fact.Subject, cancellationToken);
        HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic);
        IReadOnlyCollection<HistoricalSourceReference> resolvedSources =
            await this.LoadSourceRevisionsAsync(fact.SourceReferences, cancellationToken);
        HistoricalFactEvidenceValidator.Validate(fact, resolvedSources);
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            return HistoricalRevisionWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            HistoricalFactDocument? existing = await this.collection
                .Find(document => document.Id == candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return DocumentsMatch(existing, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }
    }

    public async Task<HistoricalFact?> GetRevisionAsync(
        Guid factId,
        int revision,
        CancellationToken cancellationToken)
    {
        if (factId == Guid.Empty || revision <= 0)
        {
            return null;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalFactDocument? document = await this.collection
            .Find(item => item.FactId == normalizedFactId && item.Revision == revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<HistoricalFact?> GetLatestRevisionAsync(
        Guid factId,
        CancellationToken cancellationToken)
    {
        if (factId == Guid.Empty)
        {
            return null;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalFactDocument? document = await this.collection
            .Find(item => item.FactId == normalizedFactId)
            .SortByDescending(item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestRevisionsForSubjectsAsync(
        IReadOnlyCollection<HistoricalSubject> subjects,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        HistoricalSubject[] distinctSubjects = subjects
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .ToArray();
        if (distinctSubjects.Length == 0)
        {
            return Array.Empty<HistoricalFact>();
        }

        FilterDefinitionBuilder<HistoricalFactDocument> builder =
            Builders<HistoricalFactDocument>.Filter;
        FilterDefinition<HistoricalFactDocument>[] subjectFilters = distinctSubjects
            .Select(subject => builder.Eq(document => document.Subject.Type, subject.Type)
                & builder.Eq(document => document.Subject.Id, subject.Id))
            .ToArray();
        SortDefinition<HistoricalFactDocument> sort =
            Builders<HistoricalFactDocument>.Sort
                .Ascending(static document => document.FactId)
                .Descending(static document => document.Revision);
        List<HistoricalFactDocument> documents = await this.collection
            .Aggregate()
            .Match(builder.Or(subjectFilters))
            .Sort(sort)
            .Group(static document => document.FactId, static revisions => revisions.First())
            .ToListAsync(cancellationToken);

        return documents
            .Select(static document => document.ToDomain())
            .ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParkAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(publicCurrentSubjects);
        List<BsonDocument> stages = BuildLatestDecisionEligibleForParkPipeline(
                normalizedParkId,
                publicCurrentSubjects,
                this.collectionName)
            .ToList();
        stages.Add(BuildTimelineSortStage());
        PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument> pipeline =
            PipelineDefinition<HistoricalFactDocument, HistoricalFactDocument>.Create(stages);
        List<HistoricalFactDocument> documents = await this.collection
            .Aggregate(pipeline)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParkPageAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        int page,
        int pageSize,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(publicCurrentSubjects);
        if (page < 1 || pageSize < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(page));
        }

        long offset = (long)(page - 1) * pageSize;
        List<BsonDocument> stages = BuildLatestDecisionEligibleForParkPipeline(
                normalizedParkId,
                publicCurrentSubjects,
                this.collectionName)
            .ToList();
        stages.Add(BuildTimelineSortStage());
        stages.Add(new BsonDocument("$facet", new BsonDocument
        {
            ["metadata"] = new BsonArray
            {
                new BsonDocument("$count", "total"),
            },
            ["items"] = new BsonArray
            {
                new BsonDocument("$skip", offset),
                new BsonDocument("$limit", pageSize),
            },
        }));
        PipelineDefinition<HistoricalFactDocument, BsonDocument> pipeline =
            PipelineDefinition<HistoricalFactDocument, BsonDocument>.Create(stages);
        BsonDocument? result = await this.collection
            .Aggregate(pipeline)
            .FirstOrDefaultAsync(cancellationToken);
        if (result is null)
        {
            return new PagedResult<HistoricalFact>(Array.Empty<HistoricalFact>(), page, pageSize, 0);
        }

        BsonArray metadata = result["metadata"].AsBsonArray;
        long total = metadata.Count == 0
            ? 0
            : metadata[0].AsBsonDocument["total"].ToInt64();
        HistoricalFact[] facts = result["items"].AsBsonArray
            .Select(static value => BsonSerializer.Deserialize<HistoricalFactDocument>(value.AsBsonDocument))
            .Select(static document => document.ToDomain())
            .ToArray();
        return new PagedResult<HistoricalFact>(facts, page, pageSize, total);
    }

    internal static IReadOnlyCollection<BsonDocument> BuildLatestDecisionEligibleForParkPipeline(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        string collectionName)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(publicCurrentSubjects);
        string normalizedCollectionName = collectionName?.Trim() ?? string.Empty;
        if (normalizedCollectionName.Length == 0)
        {
            throw new ArgumentException("A historical facts collection name is required.", nameof(collectionName));
        }

        BsonDocument[] publicSubjectFilters = publicCurrentSubjects
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .Select(static subject => new BsonDocument
            {
                ["subject.type"] = subject.Type.ToString(),
                ["subject.id"] = subject.Id,
            })
            .ToArray();
        BsonArray publicEligibilityFilters = new BsonArray(
            publicSubjectFilters.Select(static filter => filter.DeepClone()));
        publicEligibilityFilters.Add(new BsonDocument
        {
            ["subject.publicationPolicy"] = HistoricalSubjectPublicationPolicy.HistoricalOnly.ToString(),
            ["subject.contextParkId"] = normalizedParkId,
        });

        return new BsonDocument[]
        {
            new BsonDocument("$match", BuildParkCandidateScopeFilter(
                normalizedParkId,
                publicCurrentSubjects)),
            new BsonDocument("$group", new BsonDocument
            {
                ["_id"] = "$factId",
            }),
            new BsonDocument("$lookup", new BsonDocument
            {
                ["from"] = normalizedCollectionName,
                ["let"] = new BsonDocument("candidateFactId", "$_id"),
                ["pipeline"] = new BsonArray
                {
                    new BsonDocument("$match", new BsonDocument(
                        "$expr",
                        new BsonDocument("$eq", new BsonArray
                        {
                            "$factId",
                            "$$candidateFactId",
                        }))),
                    new BsonDocument("$sort", new BsonDocument("revision", -1)),
                    new BsonDocument("$limit", 1),
                },
                ["as"] = "latestRevision",
            }),
            new BsonDocument("$unwind", "$latestRevision"),
            new BsonDocument("$replaceRoot", new BsonDocument("newRoot", "$latestRevision")),
            new BsonDocument("$match", new BsonDocument
            {
                ["publicationState"] = HistoricalPublicationState.Published.ToString(),
                ["state"] = new BsonDocument("$in", new BsonArray
                {
                    HistoricalFactState.Verified.ToString(),
                    HistoricalFactState.Probable.ToString(),
                    HistoricalFactState.Disputed.ToString(),
                }),
                ["$or"] = publicEligibilityFilters,
            }),
        };
    }

    internal static BsonDocument BuildParkCandidateScopeFilter(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(publicCurrentSubjects);
        BsonArray scopeFilters = new BsonArray(publicCurrentSubjects
            .DistinctBy(static subject => (subject.Type, subject.Id))
            .Select(static subject => new BsonDocument
            {
                ["subject.type"] = subject.Type.ToString(),
                ["subject.id"] = subject.Id,
            }));
        scopeFilters.Add(new BsonDocument("subject.contextParkId", normalizedParkId));
        return new BsonDocument("$or", scopeFilters);
    }

    private static BsonDocument BuildTimelineSortStage()
    {
        return new BsonDocument("$sort", new BsonDocument
        {
            ["timelineSortOrdinal"] = 1,
            ["sequenceWithinDate"] = 1,
            ["subject.historicalLabel"] = 1,
            ["type"] = 1,
            ["factId"] = 1,
        });
    }

    private static string NormalizeParkId(string parkId)
    {
        string normalizedParkId = parkId?.Trim() ?? string.Empty;
        if (normalizedParkId.Length == 0)
        {
            throw new ArgumentException("A park identifier is required.", nameof(parkId));
        }

        return normalizedParkId;
    }

    private static bool DocumentsMatch(
        HistoricalFactDocument? existing,
        HistoricalFactDocument candidate)
    {
        return existing is not null
            && existing.ToBsonDocument().Equals(candidate.ToBsonDocument());
    }

    private async Task<HistoricalFactDocument?> LoadPredecessorDocumentAsync(
        HistoricalFact fact,
        CancellationToken cancellationToken)
    {
        if (!fact.SupersedesRevision.HasValue)
        {
            return null;
        }

        string normalizedFactId = fact.Id.ToString("N", CultureInfo.InvariantCulture);
        int predecessorRevision = fact.SupersedesRevision.Value;
        return await this.collection
            .Find(document => document.FactId == normalizedFactId
                && document.Revision == predecessorRevision)
            .FirstOrDefaultAsync(cancellationToken);
    }

    private async Task<IReadOnlyCollection<HistoricalSourceReference>> LoadSourceRevisionsAsync(
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        CancellationToken cancellationToken)
    {
        if (sourceReferences.Count == 0)
        {
            return Array.Empty<HistoricalSourceReference>();
        }

        FilterDefinitionBuilder<HistoricalSourceDocument> builder =
            Builders<HistoricalSourceDocument>.Filter;
        FilterDefinition<HistoricalSourceDocument>[] revisionFilters = sourceReferences
            .Select(sourceReference =>
                builder.Eq(
                    document => document.SourceId,
                    sourceReference.SourceId.ToString("N", CultureInfo.InvariantCulture))
                & builder.Eq(document => document.Revision, sourceReference.Revision))
            .ToArray();
        List<HistoricalSourceDocument> sources = await this.sourceCollection
            .Find(builder.Or(revisionFilters))
            .Limit(sourceReferences.Count)
            .ToListAsync(cancellationToken);
        return sources.Select(static source => source.ToDomain()).ToArray();
    }
}
