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

public sealed class HistoricalReviewEventRepository : IHistoricalAuditWriter, IHistoricalAuditReader
{
    internal const int MaximumPageSize = 500;

    private readonly IMongoCollection<HistoricalReviewEventDocument> collection;

    public HistoricalReviewEventRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<HistoricalReviewEventDocument>(
            settings.HistoricalReviewEventsCollectionName);
    }

    public async Task<HistoricalRevisionWriteDisposition> AppendAsync(
        HistoricalReviewEvent reviewEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        HistoricalReviewEventDocument candidate = reviewEvent.ToDocument();
        try
        {
            await this.collection.InsertOneAsync(candidate, cancellationToken: cancellationToken);
            return HistoricalRevisionWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            HistoricalReviewEventDocument? existing = await this.collection
                .Find(document => document.Id == candidate.Id)
                .FirstOrDefaultAsync(cancellationToken);
            return DocumentsMatch(existing, candidate)
                ? HistoricalRevisionWriteDisposition.AlreadyExists
                : HistoricalRevisionWriteDisposition.Conflict;
        }
    }

    public async Task<IReadOnlyCollection<HistoricalReviewEvent>> ListAsync(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int limit,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceType));
        }

        if (resourceId == Guid.Empty || limit <= 0)
        {
            return Array.Empty<HistoricalReviewEvent>();
        }

        int safeLimit = Math.Min(limit, MaximumPageSize);
        string normalizedResourceId = resourceId.ToString("N", CultureInfo.InvariantCulture);
        List<HistoricalReviewEventDocument> documents = await this.collection
            .Find(document => document.ResourceType == resourceType
                && document.ResourceId == normalizedResourceId)
            .SortByDescending(document => document.OccurredAtUtc)
            .ThenByDescending(document => document.Id)
            .Limit(safeLimit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    private static bool DocumentsMatch(
        HistoricalReviewEventDocument? existing,
        HistoricalReviewEventDocument candidate)
    {
        return existing is not null
            && existing.ToBsonDocument().Equals(candidate.ToBsonDocument());
    }
}
