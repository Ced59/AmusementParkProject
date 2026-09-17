using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.FactualEvents.Models;
using AmusementPark.Application.Features.FactualEvents.Ports;
using AmusementPark.Core.Domain.FactualEvents;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class FactualChangeEventRepository : IFactualChangeEventRepository
{
    private readonly IMongoCollection<FactualChangeEventDocument> collection;

    public FactualChangeEventRepository(IMongoDatabase database, MongoDbSettings settings)
        : this(GetCollection(database, settings))
    {
    }

    internal FactualChangeEventRepository(
        IMongoCollection<FactualChangeEventDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<FactualChangeEventWriteDisposition> CreateAsync(
        FactualChangeEvent factualEvent,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        try
        {
            await this.collection.InsertOneAsync(
                factualEvent.ToDocument(),
                cancellationToken: cancellationToken);
            return FactualChangeEventWriteDisposition.Created;
        }
        catch (MongoWriteException exception)
            when (exception.WriteError?.Category == ServerErrorCategory.DuplicateKey)
        {
            FactualChangeEvent? existing = await this.GetByLogicalRevisionAsync(
                factualEvent.DeduplicationKey,
                factualEvent.Revision,
                cancellationToken);
            return existing is not null && HasSameFact(existing, factualEvent)
                ? FactualChangeEventWriteDisposition.AlreadyExists
                : FactualChangeEventWriteDisposition.Conflict;
        }
    }

    public async Task<FactualChangeEvent?> GetByLogicalRevisionAsync(
        string deduplicationKey,
        long sourceRevision,
        CancellationToken cancellationToken)
    {
        string normalizedKey = deduplicationKey?.Trim() ?? string.Empty;
        if (normalizedKey.Length == 0)
        {
            throw new ArgumentException("A deduplication key is required.", nameof(deduplicationKey));
        }

        if (sourceRevision < 1)
        {
            return null;
        }

        FilterDefinition<FactualChangeEventDocument> filter =
            Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.DeduplicationKey,
                normalizedKey)
            & Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Revision,
                sourceRevision);
        FactualChangeEventDocument? document = await this.collection.Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<FactualChangeEvent?> GetAsync(
        FactualChangeEventId eventId,
        CancellationToken cancellationToken)
    {
        FactualChangeEventDocument? document = await this.collection
            .Find(Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Id,
                eventId.Value))
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<IReadOnlyCollection<FactualChangeEvent>> GetManyAsync(
        IReadOnlyCollection<FactualChangeEventId> eventIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(eventIds);
        string[] ids = eventIds.Select(static eventId => eventId.Value).Distinct(StringComparer.Ordinal).ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<FactualChangeEvent>();
        }

        List<FactualChangeEventDocument> documents = await this.collection.Find(
            Builders<FactualChangeEventDocument>.Filter.In(static value => value.Id, ids))
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<FactualChangeEvent>> ListPublishedAsync(
        PublishedFactualEventCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        FilterDefinitionBuilder<FactualChangeEventDocument> filters = Builders<FactualChangeEventDocument>.Filter;
        FilterDefinition<FactualChangeEventDocument> filter = filters.Eq(
            static value => value.Status,
            FactualChangeStatus.Published)
            & filters.Ne(static value => value.PublishedAtUtc, null);
        if (after is not null)
        {
            filter &= filters.Or(
                filters.Gt(static value => value.PublishedAtUtc, after.PublishedAtUtc),
                filters.Eq(static value => value.PublishedAtUtc, after.PublishedAtUtc)
                    & filters.Gt(static value => value.Id, after.EventId));
        }

        List<FactualChangeEventDocument> documents = await this.collection.Find(filter)
            .SortBy(static value => value.PublishedAtUtc)
            .ThenBy(static value => value.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<FactualChangeEvent>> ListTerminalAsync(
        TerminalFactualEventCursor? after,
        int limit,
        CancellationToken cancellationToken)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(limit);
        FilterDefinitionBuilder<FactualChangeEventDocument> filters = Builders<FactualChangeEventDocument>.Filter;
        FilterDefinition<FactualChangeEventDocument> filter = filters.In(
            static value => value.Status,
            new[] { FactualChangeStatus.Corrected, FactualChangeStatus.Retracted })
            & filters.Ne(static value => value.TerminalAtUtc, null);
        if (after is not null)
        {
            filter &= filters.Or(
                filters.Gt(static value => value.TerminalAtUtc, after.TerminalAtUtc),
                filters.Eq(static value => value.TerminalAtUtc, after.TerminalAtUtc)
                    & filters.Gt(static value => value.Id, after.EventId));
        }

        List<FactualChangeEventDocument> documents = await this.collection.Find(filter)
            .SortBy(static value => value.TerminalAtUtc)
            .ThenBy(static value => value.Id)
            .Limit(limit)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<IReadOnlyCollection<FactualChangeEvent>> GetCorrectedBySuccessorIdsAsync(
        IReadOnlyCollection<FactualChangeEventId> successorEventIds,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(successorEventIds);
        string[] ids = successorEventIds
            .Select(static eventId => eventId.Value)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (ids.Length == 0)
        {
            return Array.Empty<FactualChangeEvent>();
        }

        FilterDefinition<FactualChangeEventDocument> filter =
            Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Status,
                FactualChangeStatus.Corrected)
            & Builders<FactualChangeEventDocument>.Filter.In(
                static value => value.SupersededByEventId,
                ids);
        List<FactualChangeEventDocument> documents = await this.collection.Find(filter)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    public async Task<PagedResult<FactualChangeEvent>> SearchAsync(
        FactualChangeEventSearchCriteria criteria,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        FilterDefinition<FactualChangeEventDocument> filter = BuildSearchFilter(criteria);
        long totalItems = await this.collection.CountDocumentsAsync(
            filter,
            cancellationToken: cancellationToken);
        int skip = checked((criteria.Paging.Page - 1) * criteria.Paging.PageSize);
        List<FactualChangeEventDocument> documents = await this.collection
            .Find(filter)
            .SortByDescending(static value => value.CreatedAt)
            .ThenBy(static value => value.Id)
            .Skip(skip)
            .Limit(criteria.Paging.PageSize)
            .ToListAsync(cancellationToken);
        FactualChangeEvent[] items = documents
            .Select(static document => document.ToDomain())
            .ToArray();
        return new PagedResult<FactualChangeEvent>(
            items,
            criteria.Paging.Page,
            criteria.Paging.PageSize,
            totalItems);
    }

    public async Task<FactualChangeEventMutationOutcome> ReplaceAsync(
        FactualChangeEvent factualEvent,
        long expectedVersion,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(factualEvent);
        FilterDefinition<FactualChangeEventDocument> filter =
            Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Id,
                factualEvent.Id.Value)
            & Builders<FactualChangeEventDocument>.Filter.Eq(
                static value => value.Version,
                expectedVersion);
        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            filter,
            factualEvent.ToDocument(),
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1
            ? FactualChangeEventMutationOutcome.Success
            : FactualChangeEventMutationOutcome.Conflict;
    }

    internal static FilterDefinition<FactualChangeEventDocument> BuildSearchFilter(
        FactualChangeEventSearchCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);
        FilterDefinitionBuilder<FactualChangeEventDocument> filters =
            Builders<FactualChangeEventDocument>.Filter;
        FilterDefinition<FactualChangeEventDocument> filter = filters.Empty;
        if (criteria.Status.HasValue)
        {
            filter &= filters.Eq(static value => value.Status, criteria.Status.Value);
        }

        if (criteria.TargetType.HasValue)
        {
            filter &= filters.Eq(static value => value.Target.Type, criteria.TargetType.Value);
        }

        if (criteria.EventType.HasValue)
        {
            filter &= filters.Eq(static value => value.Type, criteria.EventType.Value);
        }

        if (criteria.Confidence.HasValue)
        {
            filter &= filters.Eq(static value => value.Confidence, criteria.Confidence.Value);
        }

        return filter;
    }

    private static bool HasSameFact(FactualChangeEvent left, FactualChangeEvent right)
    {
        return left.Id == right.Id
            && left.Type == right.Type
            && left.DefinitionVersion == right.DefinitionVersion
            && left.Target == right.Target
            && left.PreviousValue == right.PreviousValue
            && left.NewValue == right.NewValue
            && left.Source == right.Source
            && left.Confidence == right.Confidence
            && left.OccurredAtUtc == right.OccurredAtUtc
            && left.HasSameLogicalRevisionAs(right)
            && left.Status == right.Status;
    }

    private static IMongoCollection<FactualChangeEventDocument> GetCollection(
        IMongoDatabase database,
        MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        return database.GetCollection<FactualChangeEventDocument>(
            settings.FactualChangeEventsCollectionName);
    }
}
