using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class HistoryEventRepository : IHistoryEventRepository
{
    private readonly IMongoCollection<HistoryEventDocument> collection;

    public HistoryEventRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<HistoryEventDocument>(settings.HistoryEventsCollectionName);
    }

    public async Task<HistoryEvent?> GetByIdAsync(string eventId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter = Builders<HistoryEventDocument>.Filter.Eq(document => document.Id, eventId);
        if (!includeHidden)
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        HistoryEventDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<HistoryEvent?> GetByOwnerKeyAsync(HistoryEntityType entityType, string ownerId, string key, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.EntityType, entityType) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.OwnerId, ownerId) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.Key, key);

        HistoryEventDocument? document = await this.collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<PagedResult<HistoryEvent>> GetAdminPageAsync(int page, int pageSize, HistoryEntityType? entityType, string? ownerId, string? search, CancellationToken cancellationToken)
    {
        int safePage = Math.Max(1, page);
        int safePageSize = Math.Clamp(pageSize, 1, 100);
        FilterDefinition<HistoryEventDocument> filter = Builders<HistoryEventDocument>.Filter.Empty;

        if (entityType.HasValue)
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.EntityType, entityType.Value);
        }

        if (!string.IsNullOrWhiteSpace(ownerId))
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.OwnerId, ownerId.Trim());
        }

        if (!string.IsNullOrWhiteSpace(search))
        {
            BsonRegularExpression expression = new BsonRegularExpression(Regex.Escape(search.Trim()), "i");
            filter &= Builders<HistoryEventDocument>.Filter.Or(
                Builders<HistoryEventDocument>.Filter.Regex(document => document.Key, expression),
                Builders<HistoryEventDocument>.Filter.Regex(document => document.EventType, expression),
                Builders<HistoryEventDocument>.Filter.Regex("titles.value", expression),
                Builders<HistoryEventDocument>.Filter.Regex("summaries.value", expression));
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);
        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Sort(BuildTimelineSort())
            .Skip((safePage - 1) * safePageSize)
            .Limit(safePageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<HistoryEvent>(
            documents.Select(static document => document.ToDomain()).ToList(),
            safePage,
            safePageSize,
            totalItems);
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetOwnerTimelineAsync(HistoryEntityType entityType, string ownerId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.EntityType, entityType) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.OwnerId, ownerId);

        if (!includeHidden)
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Sort(BuildTimelineSort())
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetParkTimelineAsync(string parkId, bool includeHidden, bool includeParkItemEvents, IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<HistoryEventDocument> builder = Builders<HistoryEventDocument>.Filter;
        FilterDefinition<HistoryEventDocument> parkEventsFilter =
            builder.Eq(document => document.EntityType, HistoryEntityType.Park) &
            builder.Eq(document => document.OwnerId, parkId);

        FilterDefinition<HistoryEventDocument> filter = parkEventsFilter;
        if (includeParkItemEvents)
        {
            FilterDefinition<HistoryEventDocument> parkItemEventsFilter =
                builder.Eq(document => document.EntityType, HistoryEntityType.ParkItem) &
                builder.Eq(document => document.ContextParkId, parkId);

            List<string> normalizedParkItemIds = parkItemIds
                .Where(static id => !string.IsNullOrWhiteSpace(id))
                .Select(static id => id.Trim())
                .Distinct(StringComparer.Ordinal)
                .ToList();

            if (normalizedParkItemIds.Count > 0)
            {
                parkItemEventsFilter &= builder.In(document => document.OwnerId, normalizedParkItemIds);
            }

            filter = builder.Or(parkEventsFilter, parkItemEventsFilter);
        }

        if (!includeHidden)
        {
            filter &= builder.Eq(document => document.IsVisible, true);
        }

        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Sort(BuildTimelineSort())
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetPublicVisibleEventsAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<HistoryEvent>();
        }

        FilterDefinition<HistoryEventDocument> filter = Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true);

        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Sort(BuildTimelineSort())
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetPublicSitemapCandidatesAsync(int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<HistoryEvent>();
        }

        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.IsMajor, true) &
            Builders<HistoryEventDocument>.Filter.Ne(document => document.Article, null) &
            Builders<HistoryEventDocument>.Filter.Eq("article.isPublished", true);

        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Sort(BuildTimelineSort())
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<HistoryEvent> CreateAsync(HistoryEvent historyEvent, CancellationToken cancellationToken)
    {
        HistoryEventDocument document = historyEvent.ToDocument();
        document.CreatedAt = DateTime.UtcNow;
        document.UpdatedAt = document.CreatedAt;

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
        return document.ToDomain();
    }

    public async Task<HistoryEvent?> UpdateAsync(string eventId, HistoryEvent historyEvent, CancellationToken cancellationToken)
    {
        HistoryEventDocument? existing = await this.collection.Find(document => document.Id == eventId)
            .Project(static document => new HistoryEventDocument
            {
                Id = document.Id,
                CreatedAt = document.CreatedAt,
            })
            .FirstOrDefaultAsync(cancellationToken);

        if (existing is null)
        {
            return null;
        }

        HistoryEventDocument document = historyEvent.ToDocument();
        document.Id = eventId;
        document.CreatedAt = existing.CreatedAt;
        document.UpdatedAt = DateTime.UtcNow;

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            current => current.Id == eventId,
            document,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 0 ? null : document.ToDomain();
    }

    public async Task<bool> DeleteAsync(string eventId, CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(document => document.Id == eventId, cancellationToken);
        return result.DeletedCount > 0;
    }

    private static SortDefinition<HistoryEventDocument> BuildTimelineSort()
    {
        return Builders<HistoryEventDocument>.Sort
            .Ascending(document => document.Year)
            .Ascending(document => document.Month)
            .Ascending(document => document.Day)
            .Ascending(document => document.Key)
            .Ascending(document => document.Id);
    }
}
