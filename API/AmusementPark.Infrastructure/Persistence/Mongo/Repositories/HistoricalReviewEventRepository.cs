using System.Globalization;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoricalReviewEventRepository : IHistoricalAuditReader
{
    internal const int MaximumPageSize = 500;

    private readonly IMongoCollection<HistoricalFactDocument> factCollection;
    private readonly IMongoCollection<HistoricalSourceDocument> sourceCollection;

    public HistoricalReviewEventRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.factCollection = database.GetCollection<HistoricalFactDocument>(
            settings.HistoricalFactsCollectionName);
        this.sourceCollection = database.GetCollection<HistoricalSourceDocument>(
            settings.HistoricalSourcesCollectionName);
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
        switch (resourceType)
        {
            case HistoricalReviewResourceType.Fact:
                List<HistoricalReviewEventDocument> factReviewEvents = await this.factCollection
                    .Find(document => document.FactId == normalizedResourceId)
                    .SortByDescending(document => document.TransitionReviewEvent.OccurredAtUtc)
                    .ThenByDescending(document => document.Revision)
                    .Limit(safeLimit)
                    .Project(document => document.TransitionReviewEvent)
                    .ToListAsync(cancellationToken);
                return factReviewEvents
                    .Select(static document => document.ToDomain())
                    .ToArray();
            case HistoricalReviewResourceType.Source:
                List<HistoricalReviewEventDocument> sourceReviewEvents = await this.sourceCollection
                    .Find(document => document.SourceId == normalizedResourceId)
                    .SortByDescending(document => document.TransitionReviewEvent.OccurredAtUtc)
                    .ThenByDescending(document => document.Revision)
                    .Limit(safeLimit)
                    .Project(document => document.TransitionReviewEvent)
                    .ToListAsync(cancellationToken);
                return sourceReviewEvents
                    .Select(static document => document.ToDomain())
                    .ToArray();
            default:
                throw new HistoricalPersistenceValidationException(
                    HistoricalPersistenceErrorCodes.InvalidReviewEvent,
                    "The historical review event resource type is not readable yet.");
        }
    }
}
