using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.History.Models;
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
        this.collection = database.GetCollection<HistoryEventDocument>(
            settings.HistoricalNarrativesCollectionName);
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

    public async Task<IReadOnlyCollection<HistoryEvent>> GetPublishedArticlesByIdsAsync(
        IReadOnlyCollection<string> eventIds,
        CancellationToken cancellationToken)
    {
        string[] normalizedIds = eventIds
            .Where(static eventId => !string.IsNullOrWhiteSpace(eventId))
            .Select(static eventId => eventId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (normalizedIds.Length == 0)
        {
            return Array.Empty<HistoryEvent>();
        }

        FilterDefinitionBuilder<HistoryEventDocument> builder = Builders<HistoryEventDocument>.Filter;
        FilterDefinition<HistoryEventDocument> filter =
            builder.In(document => document.Id, normalizedIds)
            & builder.Eq(document => document.IsVisible, true)
            & builder.Eq(document => document.IsMajor, true)
            & builder.Ne(document => document.Article, null)
            & builder.Eq("article.isPublished", true);
        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .Project<HistoryEventDocument>(BuildTimelineProjection())
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToArray();
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

        return await this.GetTimelineAsync(filter, true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetOwnerTimelineSummaryAsync(HistoryEntityType entityType, string ownerId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.EntityType, entityType) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.OwnerId, ownerId);

        if (!includeHidden)
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return await this.GetTimelineAsync(filter, false, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetOwnerTimelinesAsync(HistoryEntityType entityType, IReadOnlyCollection<string> ownerIds, bool includeHidden, CancellationToken cancellationToken)
    {
        List<string> normalizedOwnerIds = ownerIds
            .Where(static ownerId => !string.IsNullOrWhiteSpace(ownerId))
            .Select(static ownerId => ownerId.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToList();

        if (normalizedOwnerIds.Count == 0)
        {
            return Array.Empty<HistoryEvent>();
        }

        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.EntityType, entityType) &
            Builders<HistoryEventDocument>.Filter.In(document => document.OwnerId, normalizedOwnerIds);

        if (!includeHidden)
        {
            filter &= Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true);
        }

        return await this.GetTimelineAsync(filter, true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetParkTimelineAsync(string parkId, bool includeHidden, bool includeParkItemEvents, IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter = BuildParkTimelineFilter(parkId, includeHidden, includeParkItemEvents, parkItemIds);
        return await this.GetTimelineAsync(filter, true, cancellationToken);
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetParkTimelineSummaryAsync(string parkId, bool includeHidden, bool includeParkItemEvents, IReadOnlyCollection<string> parkItemIds, CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter = BuildParkTimelineFilter(parkId, includeHidden, includeParkItemEvents, parkItemIds);
        return await this.GetTimelineAsync(filter, false, cancellationToken);
    }

    public async Task<bool> HasParkItemTimelineEventsAsync(string parkId, bool includeHidden, CancellationToken cancellationToken)
    {
        FilterDefinitionBuilder<HistoryEventDocument> builder = Builders<HistoryEventDocument>.Filter;
        FilterDefinition<HistoryEventDocument> filter =
            builder.Eq(document => document.EntityType, HistoryEntityType.ParkItem) &
            builder.Eq(document => document.ContextParkId, parkId);

        if (!includeHidden)
        {
            filter &= builder.Eq(document => document.IsVisible, true);
        }

        long count = await this.collection.CountDocumentsAsync(filter, new CountOptions { Limit = 1 }, cancellationToken);
        return count > 0;
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
            .Project<HistoryEventDocument>(BuildTimelineProjection())
            .Limit(limit)
            .ToListAsync(cancellationToken);

        return documents.Select(static document => document.ToDomain()).ToList();
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetLatestPublishedArticlesAsync(int offset, int limit, CancellationToken cancellationToken)
    {
        if (limit <= 0)
        {
            return Array.Empty<HistoryEvent>();
        }

        int normalizedOffset = Math.Max(0, offset);
        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.IsVisible, true) &
            Builders<HistoryEventDocument>.Filter.Eq(document => document.IsMajor, true) &
            Builders<HistoryEventDocument>.Filter.In(
                document => document.EntityType,
                new[] { HistoryEntityType.Park, HistoryEntityType.ParkItem }) &
            Builders<HistoryEventDocument>.Filter.Ne(document => document.Article, null) &
            Builders<HistoryEventDocument>.Filter.Eq("article.isPublished", true);

        List<HistoryEventDocument> documents = await this.collection.Find(filter)
            .SortByDescending(document => document.UpdatedAt)
            .ThenByDescending(document => document.CreatedAt)
            .ThenBy(document => document.Id)
            .Project<HistoryEventDocument>(BuildTimelineProjection())
            .Skip(normalizedOffset)
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
            .Project<HistoryEventDocument>(BuildTimelineProjection())
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

    public async Task<HistoryEvent?> UpdateAsync(
        string eventId,
        HistoryEvent historyEvent,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        Guid mutationId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> mutationFilter = BuildConditionalUpdateFilter(
            eventId,
            expectedUpdatedAtUtc,
            expectedCanonicalFactId);
        HistoryEventDocument? existing = await this.collection.Find(mutationFilter)
            .Project(static document => new HistoryEventDocument
            {
                Id = document.Id,
                CreatedAt = document.CreatedAt,
                CanonicalFactId = document.CanonicalFactId,
                CanonicalizationState = document.CanonicalizationState,
                MigrationVersion = document.MigrationVersion,
                MigrationWarnings = document.MigrationWarnings,
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
        document.CanonicalFactId = existing.CanonicalFactId;
        document.CanonicalizationState = HistoricalNarrativeCanonicalizationState.PendingReview;
        document.MigrationVersion = existing.MigrationVersion;
        document.MigrationWarnings = existing.MigrationWarnings
            .Append("narrative-updated-after-migration")
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static warning => warning, StringComparer.Ordinal)
            .ToList();
        document.LastMutationId = mutationId.ToString("N");

        ReplaceOneResult result = await this.collection.ReplaceOneAsync(
            mutationFilter,
            document,
            cancellationToken: cancellationToken);

        return result.MatchedCount == 0 ? null : document.ToDomain();
    }

    public async Task<HistoryEvent?> GetCommittedUpdateAsync(
        string eventId,
        Guid mutationId,
        CancellationToken cancellationToken)
    {
        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.Id, eventId)
            & Builders<HistoryEventDocument>.Filter.Eq(
                document => document.LastMutationId,
                mutationId.ToString("N"));
        HistoryEventDocument? document = await this.collection
            .Find(filter)
            .FirstOrDefaultAsync(cancellationToken);
        return document?.ToDomain();
    }

    public async Task<HistoryEventMutationSnapshot?> GetMutationSnapshotAsync(
        string eventId,
        CancellationToken cancellationToken)
    {
        HistoryEventDocument? document = await this.collection
            .Find(item => item.Id == eventId)
            .FirstOrDefaultAsync(cancellationToken);
        if (document is null)
        {
            return null;
        }

        Guid? lastMutationId = Guid.TryParseExact(
            document.LastMutationId,
            "N",
            out Guid parsedMutationId)
            ? parsedMutationId
            : null;
        return new HistoryEventMutationSnapshot(document.ToDomain(), lastMutationId);
    }

    internal static FilterDefinition<HistoryEventDocument> BuildConditionalUpdateFilter(
        string eventId,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId)
    {
        return Builders<HistoryEventDocument>.Filter.Eq(document => document.Id, eventId)
            & Builders<HistoryEventDocument>.Filter.Eq(
                document => document.UpdatedAt,
                expectedUpdatedAtUtc)
            & Builders<HistoryEventDocument>.Filter.Eq(
                document => document.CanonicalFactId,
                expectedCanonicalFactId?.ToString("N"));
    }

    public async Task<IReadOnlyCollection<HistoryEvent>> GetCanonicalizationCandidatesAsync(
        string canonicalizationVersion,
        CancellationToken cancellationToken)
    {
        string normalizedVersion = canonicalizationVersion?.Trim() ?? string.Empty;
        if (normalizedVersion.Length == 0)
        {
            throw new ArgumentException(
                "A historical canonicalization version is required.",
                nameof(canonicalizationVersion));
        }

        FilterDefinition<HistoryEventDocument> filter = BuildCanonicalizationCandidateFilter(
            normalizedVersion);
        List<HistoryEventDocument> documents = await this.collection
            .Find(filter)
            .SortBy(static document => document.Id)
            .ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToArray();
    }

    internal static FilterDefinition<HistoryEventDocument> BuildCanonicalizationCandidateFilter(
        string canonicalizationVersion)
    {
        return
            Builders<HistoryEventDocument>.Filter.Ne(
                document => document.MigrationVersion,
                canonicalizationVersion)
            | Builders<HistoryEventDocument>.Filter.Exists(
                document => document.MigrationVersion,
                false)
            | Builders<HistoryEventDocument>.Filter.Nin(
                document => document.CanonicalizationState,
                new[]
                {
                    HistoricalNarrativeCanonicalizationState.Canonicalized,
                    HistoricalNarrativeCanonicalizationState.Blocked,
                });
    }

    public async Task<bool> SetCanonicalizationAsync(
        string eventId,
        DateTime expectedUpdatedAtUtc,
        Guid? canonicalFactId,
        HistoricalNarrativeCanonicalizationState state,
        string canonicalizationVersion,
        IReadOnlyCollection<string> warnings,
        CancellationToken cancellationToken)
    {
        string normalizedEventId = eventId?.Trim() ?? string.Empty;
        string normalizedVersion = canonicalizationVersion?.Trim() ?? string.Empty;
        if (normalizedEventId.Length == 0 || normalizedVersion.Length == 0)
        {
            return false;
        }

        FilterDefinition<HistoryEventDocument> filter =
            Builders<HistoryEventDocument>.Filter.Eq(document => document.Id, normalizedEventId)
            & Builders<HistoryEventDocument>.Filter.Eq(
                document => document.UpdatedAt,
                expectedUpdatedAtUtc);
        UpdateDefinition<HistoryEventDocument> update = Builders<HistoryEventDocument>.Update
            .Set(document => document.CanonicalizationState, state)
            .Set(document => document.MigrationVersion, normalizedVersion)
            .Set(document => document.MigrationWarnings, warnings
                .Where(static warning => !string.IsNullOrWhiteSpace(warning))
                .Select(static warning => warning.Trim())
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static warning => warning, StringComparer.Ordinal)
                .ToList());
        update = canonicalFactId.HasValue
            ? update.Set(
                document => document.CanonicalFactId,
                canonicalFactId.Value.ToString("N"))
            : update.Unset(document => document.CanonicalFactId);
        UpdateResult result = await this.collection.UpdateOneAsync(
            filter,
            update,
            cancellationToken: cancellationToken);
        return result.MatchedCount == 1;
    }

    public async Task<bool> DeleteAsync(
        string eventId,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        CancellationToken cancellationToken)
    {
        DeleteResult result = await this.collection.DeleteOneAsync(
            BuildConditionalUpdateFilter(
                eventId,
                expectedUpdatedAtUtc,
                expectedCanonicalFactId),
            cancellationToken);
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

    private async Task<IReadOnlyCollection<HistoryEvent>> GetTimelineAsync(FilterDefinition<HistoryEventDocument> filter, bool includeArticleDetails, CancellationToken cancellationToken)
    {
        IFindFluent<HistoryEventDocument, HistoryEventDocument> find = this.collection.Find(filter)
            .Sort(BuildTimelineSort());

        if (!includeArticleDetails)
        {
            find = find.Project<HistoryEventDocument>(BuildTimelineProjection());
        }

        List<HistoryEventDocument> documents = await find.ToListAsync(cancellationToken);
        return documents.Select(static document => document.ToDomain()).ToList();
    }

    private static FilterDefinition<HistoryEventDocument> BuildParkTimelineFilter(
        string parkId,
        bool includeHidden,
        bool includeParkItemEvents,
        IReadOnlyCollection<string> parkItemIds)
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

        return filter;
    }

    private static ProjectionDefinition<HistoryEventDocument> BuildTimelineProjection()
    {
        return Builders<HistoryEventDocument>.Projection
            .Exclude("article.blocks")
            .Exclude("article.sources");
    }
}
