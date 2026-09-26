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

    public HistoricalFactRepository(
        IMongoDatabase database,
        MongoDbSettings settings,
        IHistoricalSubjectPublicationStateReader subjectPublicationStateReader)
    {
        this.collection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
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
