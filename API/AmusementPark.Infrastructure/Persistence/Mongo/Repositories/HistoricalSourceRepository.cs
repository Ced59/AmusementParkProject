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
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(source);
        HistoricalSourceDocument candidate = source.ToDocument();
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

        FilterDefinition<HistoricalSourceDocument> filter = Builders<HistoricalSourceDocument>.Filter
            .In(document => document.SourceId, normalizedSourceIds);
        List<HistoricalSourceDocument> documents = await this.collection
            .Find(filter)
            .SortBy(document => document.SourceId)
            .ThenByDescending(document => document.Revision)
            .ToListAsync(cancellationToken);

        return documents
            .GroupBy(static document => document.SourceId, StringComparer.Ordinal)
            .Select(static revisions => revisions.First().ToDomain())
            .ToArray();
    }

    public async Task<IReadOnlyCollection<HistoricalSourceReference>> GetRevisionsAsync(
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(sourceReferences);
        HistoricalSourceRevisionReference[] normalizedReferences = sourceReferences
            .Distinct()
            .OrderBy(static sourceReference => sourceReference.SourceId)
            .ThenBy(static sourceReference => sourceReference.Revision)
            .ToArray();
        if (normalizedReferences.Length == 0)
        {
            return Array.Empty<HistoricalSourceReference>();
        }

        if (normalizedReferences.Length > MaximumBatchSize)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sourceReferences),
                $"A historical source batch cannot exceed {MaximumBatchSize} revision references.");
        }

        FilterDefinitionBuilder<HistoricalSourceDocument> builder =
            Builders<HistoricalSourceDocument>.Filter;
        FilterDefinition<HistoricalSourceDocument>[] revisionFilters = normalizedReferences
            .Select(sourceReference =>
                builder.Eq(document => document.SourceId, sourceReference.SourceId.ToString("N", CultureInfo.InvariantCulture))
                & builder.Eq(document => document.Revision, sourceReference.Revision))
            .ToArray();
        List<HistoricalSourceDocument> documents = await this.collection
            .Find(builder.Or(revisionFilters))
            .SortBy(document => document.SourceId)
            .ThenBy(document => document.Revision)
            .Limit(normalizedReferences.Length)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    private static bool DocumentsMatch(
        HistoricalSourceDocument? existing,
        HistoricalSourceDocument candidate)
    {
        return existing is not null
            && existing.ToBsonDocument().Equals(candidate.ToBsonDocument());
    }
}
