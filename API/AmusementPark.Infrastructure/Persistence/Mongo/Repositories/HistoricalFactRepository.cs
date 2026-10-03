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

public sealed class HistoricalFactRepository : IHistoricalFactRepository
{
    private readonly IMongoCollection<HistoricalFactDocument> collection;
    private readonly IMongoCollection<HistoricalSourceDocument> sourceCollection;
    private readonly IHistoricalSubjectPublicationStateReader subjectPublicationStateReader;
    private readonly IHistoricalParkRolloutGateCache rolloutGateCache;
    private readonly string collectionName;

    public HistoricalFactRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHistoricalSubjectPublicationStateReader subjectPublicationStateReader,
        IHistoricalParkRolloutGateCache rolloutGateCache)
    {
        this.collection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
        this.collectionName = settings.HistoricalFactsCollectionName;
        this.sourceCollection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
        this.subjectPublicationStateReader = subjectPublicationStateReader
            ?? throw new ArgumentNullException(nameof(subjectPublicationStateReader));
        this.rolloutGateCache = rolloutGateCache
            ?? throw new ArgumentNullException(nameof(rolloutGateCache));
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
            && fact.PublicationState == HistoricalPublicationState.Published;
        bool currentSubjectIsPublic = !requiresCurrentSubjectResolution
            || await this.subjectPublicationStateReader.IsPublicAsync(fact.Subject, cancellationToken);
        HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic);
        IReadOnlyCollection<HistoricalSourceReference> resolvedSources =
            await this.LoadSourceRevisionsAsync(fact.SourceReferences, cancellationToken);
        HistoricalFactEvidenceValidator.Validate(fact, resolvedSources);
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            this.rolloutGateCache.Invalidate();
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

    public async Task<bool> WasLatestRevisionTransitionRecordedByAsync(
        Guid factId,
        HistoricalReviewEventType eventType,
        string actorUserId,
        CancellationToken cancellationToken)
    {
        string normalizedActorUserId = actorUserId?.Trim() ?? string.Empty;
        if (factId == Guid.Empty || normalizedActorUserId.Length == 0)
        {
            return false;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalReviewEventDocument? reviewEvent = await this.collection
            .Find(item => item.FactId == normalizedFactId)
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

    public async Task<bool> IsLatestRevisionSubjectAlignedAsync(
        Guid factId,
        HistoricalSubject expectedSubject,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(expectedSubject);
        if (factId == Guid.Empty)
        {
            return false;
        }

        string normalizedFactId = factId.ToString("N", CultureInfo.InvariantCulture);
        HistoricalFactDocument? document = await this.collection
            .Find(item => item.FactId == normalizedFactId)
            .SortByDescending(item => item.Revision)
            .FirstOrDefaultAsync(cancellationToken);
        return document is not null && IsSubjectAligned(document.Subject, expectedSubject);
    }

    internal static bool IsSubjectAligned(
        HistoricalSubjectDocument persistedSubject,
        HistoricalSubject expectedSubject)
    {
        ArgumentNullException.ThrowIfNull(persistedSubject);
        ArgumentNullException.ThrowIfNull(expectedSubject);
        return persistedSubject.Type == expectedSubject.Type
            && string.Equals(persistedSubject.Id, expectedSubject.Id, StringComparison.Ordinal)
            && string.Equals(
                persistedSubject.HistoricalLabel,
                expectedSubject.HistoricalLabel,
                StringComparison.Ordinal)
            && persistedSubject.PublicationPolicy == expectedSubject.PublicationPolicy
            && string.Equals(
                persistedSubject.ContextParkId,
                expectedSubject.ContextParkId,
                StringComparison.Ordinal);
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

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestRevisionsForParkAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> currentSubjects,
        CancellationToken cancellationToken)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(currentSubjects);
        List<BsonDocument> stages = BuildLatestForParkPipeline(
                normalizedParkId,
                currentSubjects,
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

    public async Task<IReadOnlyCollection<HistoricalFact>> GetLatestDecisionEligibleRevisionsForParkAsync(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoricalFact> latestFacts = await this.GetLatestRevisionsForParkAsync(
            parkId,
            publicCurrentSubjects,
            cancellationToken);
        return latestFacts
            .Where(static fact => fact.IsDecisionEligible)
            .ToArray();
    }

    internal static IReadOnlyCollection<BsonDocument> BuildLatestForParkPipeline(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> currentSubjects,
        string collectionName)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(currentSubjects);
        string normalizedCollectionName = collectionName?.Trim() ?? string.Empty;
        if (normalizedCollectionName.Length == 0)
        {
            throw new ArgumentException("A historical facts collection name is required.", nameof(collectionName));
        }

        return new BsonDocument[]
        {
            new BsonDocument("$match", BuildParkCandidateScopeFilter(
                normalizedParkId,
                currentSubjects)),
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
            new BsonDocument("$match", BuildParkCandidateScopeFilter(
                normalizedParkId,
                currentSubjects)),
        };
    }

    internal static BsonDocument BuildParkCandidateScopeFilter(
        string parkId,
        IReadOnlyCollection<HistoricalSubject> publicCurrentSubjects)
    {
        string normalizedParkId = NormalizeParkId(parkId);
        ArgumentNullException.ThrowIfNull(publicCurrentSubjects);
        BsonArray scopeFilters = new BsonArray(publicCurrentSubjects
            .DistinctBy(static subject => (subject.Type, subject.Id, subject.ContextParkId))
            .Select(static subject => new BsonDocument
            {
                ["subject.type"] = subject.Type.ToString(),
                ["subject.id"] = subject.Id,
                ["subject.contextParkId"] = subject.ContextParkId is null
                    ? BsonNull.Value
                    : new BsonString(subject.ContextParkId),
                ["subject.publicationPolicy"] =
                    HistoricalSubjectPublicationPolicy.FollowCurrentSubject.ToString(),
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
