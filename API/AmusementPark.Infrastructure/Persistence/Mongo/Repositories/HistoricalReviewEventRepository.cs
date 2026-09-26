using System.Globalization;
using AmusementPark.Application.Features.History.Models;
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

    public async Task<HistoricalAuditPage> ListAsync(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int pageSize,
        HistoricalAuditCursor? after,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(resourceType))
        {
            throw new ArgumentOutOfRangeException(nameof(resourceType));
        }

        if (resourceId == Guid.Empty || pageSize <= 0)
        {
            return new HistoricalAuditPage(Array.Empty<HistoricalReviewEvent>(), null);
        }

        ValidateCursor(after);
        int safePageSize = Math.Min(pageSize, MaximumPageSize);
        int queryLimit = checked(safePageSize + 1);
        string normalizedResourceId = resourceId.ToString("N", CultureInfo.InvariantCulture);
        switch (resourceType)
        {
            case HistoricalReviewResourceType.Fact:
                List<HistoricalReviewEventDocument> factReviewEvents = await this.factCollection
                    .Find(BuildFactFilter(normalizedResourceId, after))
                    .SortByDescending(document => document.TransitionReviewEvent.OccurredAtUtc)
                    .ThenByDescending(document => document.Revision)
                    .Limit(queryLimit)
                    .Project(document => document.TransitionReviewEvent)
                    .ToListAsync(cancellationToken);
                return BuildPage(factReviewEvents, safePageSize);
            case HistoricalReviewResourceType.Source:
                List<HistoricalReviewEventDocument> sourceReviewEvents = await this.sourceCollection
                    .Find(BuildSourceFilter(normalizedResourceId, after))
                    .SortByDescending(document => document.TransitionReviewEvent.OccurredAtUtc)
                    .ThenByDescending(document => document.Revision)
                    .Limit(queryLimit)
                    .Project(document => document.TransitionReviewEvent)
                    .ToListAsync(cancellationToken);
                return BuildPage(sourceReviewEvents, safePageSize);
            default:
                throw new HistoricalPersistenceValidationException(
                    HistoricalPersistenceErrorCodes.InvalidReviewEvent,
                    "The historical review event resource type is not readable yet.");
        }
    }

    internal static FilterDefinition<HistoricalFactDocument> BuildFactFilter(
        string resourceId,
        HistoricalAuditCursor? after)
    {
        FilterDefinitionBuilder<HistoricalFactDocument> builder = Builders<HistoricalFactDocument>.Filter;
        FilterDefinition<HistoricalFactDocument> filter = builder.Eq(
            static document => document.FactId,
            resourceId);
        if (after is null)
        {
            return filter;
        }

        return filter & builder.Or(
            builder.Lt(
                static document => document.TransitionReviewEvent.OccurredAtUtc,
                after.OccurredAtUtc),
            builder.And(
                builder.Eq(
                    static document => document.TransitionReviewEvent.OccurredAtUtc,
                    after.OccurredAtUtc),
                builder.Lt(static document => document.Revision, after.ResourceRevision)));
    }

    internal static FilterDefinition<HistoricalSourceDocument> BuildSourceFilter(
        string resourceId,
        HistoricalAuditCursor? after)
    {
        FilterDefinitionBuilder<HistoricalSourceDocument> builder = Builders<HistoricalSourceDocument>.Filter;
        FilterDefinition<HistoricalSourceDocument> filter = builder.Eq(
            static document => document.SourceId,
            resourceId);
        if (after is null)
        {
            return filter;
        }

        return filter & builder.Or(
            builder.Lt(
                static document => document.TransitionReviewEvent.OccurredAtUtc,
                after.OccurredAtUtc),
            builder.And(
                builder.Eq(
                    static document => document.TransitionReviewEvent.OccurredAtUtc,
                    after.OccurredAtUtc),
                builder.Lt(static document => document.Revision, after.ResourceRevision)));
    }

    internal static HistoricalAuditPage BuildPage(
        IReadOnlyCollection<HistoricalReviewEventDocument> documents,
        int pageSize)
    {
        HistoricalReviewEvent[] events = documents
            .Take(pageSize)
            .Select(static document => document.ToDomain())
            .ToArray();
        HistoricalAuditCursor? nextCursor = documents.Count > pageSize && events.Length > 0
            ? new HistoricalAuditCursor(
                events[^1].OccurredAtUtc,
                events[^1].ResourceRevision)
            : null;
        return new HistoricalAuditPage(events, nextCursor);
    }

    private static void ValidateCursor(HistoricalAuditCursor? after)
    {
        if (after is not null
            && (after.OccurredAtUtc.Kind != DateTimeKind.Utc || after.ResourceRevision < 1))
        {
            throw new ArgumentOutOfRangeException(nameof(after));
        }
    }
}
