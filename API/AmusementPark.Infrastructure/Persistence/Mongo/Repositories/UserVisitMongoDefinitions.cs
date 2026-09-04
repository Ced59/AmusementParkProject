using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserVisitMongoDefinitions
{
    public const string DeletedAtUtcPath = "deletedAtUtc";
    public const string PurgeScheduledForUtcPath = "purgeScheduledForUtc";
    public const string ExportInvalidationEnsuredAtUtcPath =
        "exportInvalidationEnsuredAtUtc";
    public const string PurgeJobEnsuredAtUtcPath = "purgeJobEnsuredAtUtc";
    public const string ContentMutationLeaseTokenPath = "contentMutationLeaseToken";
    public const string ContentMutationLeaseExpiresAtUtcPath = "contentMutationLeaseExpiresAtUtc";
    public const string ContentMutationFenceTokenPath = "contentMutationFenceToken";
    public const string ContentMutationFenceStableTokenPath =
        "contentMutationFenceStableToken";
    public const string ContentMutationFenceReadyPath = "contentMutationFenceReady";

    public static FilterDefinition<UserVisitDocument> BuildOwnerFilter(string userId)
    {
        return Builders<UserVisitDocument>.Filter.Eq(
            static document => document.UserId,
            NormalizeRequired(userId, nameof(userId)))
            & BuildNotDeletedFilter();
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedVisitFilter(
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        return BuildOwnedAnyStateVisitFilter(visitId, userId)
            & BuildNotDeletedFilter();
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedAnyStateVisitFilter(
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.Id,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)));
    }

    public static FilterDefinition<UserVisitDocument> BuildNotDeletedFilter()
    {
        return Builders<UserVisitDocument>.Filter.Eq<DateTime?>(
            DeletedAtUtcPath,
            null);
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedVersionFilter(
        string visitId,
        string userId,
        long expectedVersion)
    {
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedVersion),
                "The expected visit version must be positive.");
        }

        return BuildOwnedVisitFilter(visitId, userId)
            & Builders<UserVisitDocument>.Filter.Eq(
                static document => document.Version,
                expectedVersion);
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedMutableVersionFilter(
        string visitId,
        string userId,
        long expectedVersion,
        DateTime mutationAtUtc)
    {
        if (mutationAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The mutation timestamp must be UTC.", nameof(mutationAtUtc));
        }

        FilterDefinitionBuilder<UserVisitDocument> filters =
            Builders<UserVisitDocument>.Filter;
        return BuildOwnedVersionFilter(visitId, userId, expectedVersion)
            & filters.Or(
                filters.Exists(ContentMutationLeaseTokenPath, false),
                filters.Lte(ContentMutationLeaseExpiresAtUtcPath, mutationAtUtc));
    }

    public static FilterDefinition<UserVisitDocument> BuildOwnedLeasedVersionFilter(
        string visitId,
        string userId,
        long expectedVersion,
        string contentMutationLeaseToken)
    {
        if (string.IsNullOrWhiteSpace(contentMutationLeaseToken))
        {
            throw new ArgumentException(
                "The content mutation lease token is required.",
                nameof(contentMutationLeaseToken));
        }

        return BuildOwnedVersionFilter(visitId, userId, expectedVersion)
            & Builders<UserVisitDocument>.Filter.Eq(
                ContentMutationLeaseTokenPath,
                contentMutationLeaseToken.Trim());
    }

    public static SortDefinition<UserVisitDocument> BuildNewestVisitSort()
    {
        return Builders<UserVisitDocument>.Sort
            .Descending(static document => document.DateSortKey)
            .Descending(static document => document.UpdatedAt)
            .Ascending(static document => document.Id);
    }

    public static ProjectionDefinition<UserVisitDocument> BuildListProjection()
    {
        return Builders<UserVisitDocument>.Projection
            .Exclude(static document => document.ParkAssessment);
    }

    public static FilterDefinition<UserVisitDocument> BuildMissingDateSortKeyFilter()
    {
        return Builders<UserVisitDocument>.Filter.Exists(
            static document => document.DateSortKey,
            false);
    }

    public static UpdateDefinition<UserVisitDocument> BuildDateSortKeyBackfillUpdate()
    {
        BsonDocument chronologicalOrder = new BsonDocument(
            "$add",
            new BsonArray
            {
                new BsonDocument("$multiply", new BsonArray { "$date.year", 10000 }),
                new BsonDocument(
                    "$multiply",
                    new BsonArray
                    {
                        new BsonDocument("$ifNull", new BsonArray { "$date.month", 0 }),
                        100,
                    }),
                new BsonDocument("$ifNull", new BsonArray { "$date.day", 0 }),
            });
        return new PipelineUpdateDefinition<UserVisitDocument>(
            new[]
            {
                new BsonDocument(
                    "$set",
                    new BsonDocument("dateSortKey", chronologicalOrder)),
            });
    }

    public static FilterDefinition<UserVisitDocument> BuildCreationOperationFilter(
        string userId,
        string operationKeyHash)
    {
        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.CreationOperationKeyHash,
                NormalizeRequired(operationKeyHash, nameof(operationKeyHash)));
    }

    public static FilterDefinition<UserVisitDocument> BuildListFilter(
        UserVisitListCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        FilterDefinitionBuilder<UserVisitDocument> filters = Builders<UserVisitDocument>.Filter;
        FilterDefinition<UserVisitDocument> filter = BuildOwnerFilter(criteria.UserId);
        if (!string.IsNullOrWhiteSpace(criteria.ParkId))
        {
            filter &= filters.Eq(
                static document => document.ParkId,
                criteria.ParkId.Trim());
        }

        if (criteria.Year.HasValue)
        {
            filter &= filters.Eq(
                static document => document.Date.Year,
                criteria.Year.Value);
        }

        if (criteria.Status.HasValue)
        {
            filter &= filters.Eq(
                static document => document.Status,
                criteria.Status.Value);
        }

        if (criteria.After is not null)
        {
            int dateSortKey = criteria.After.Date.ChronologicalOrderValue;
            FilterDefinition<UserVisitDocument> afterFilter =
                filters.Lt(static document => document.DateSortKey, dateSortKey)
                | (filters.Eq(static document => document.DateSortKey, dateSortKey)
                    & filters.Lt(static document => document.UpdatedAt, criteria.After.UpdatedAtUtc))
                | (filters.Eq(static document => document.DateSortKey, dateSortKey)
                    & filters.Eq(static document => document.UpdatedAt, criteria.After.UpdatedAtUtc)
                    & filters.Gt(static document => document.Id, criteria.After.VisitId.Value));
            filter &= afterFilter;
        }

        return filter;
    }

    public static IReadOnlyCollection<CreateIndexModel<UserVisitDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<UserVisitDocument>>
        {
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.Date.Year)
                    .Descending(static document => document.Date.Month)
                    .Descending(static document => document.Date.Day),
                new CreateIndexOptions { Name = "idx_user_visits_user_date" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId)
                    .Descending(static document => document.Date.Year),
                new CreateIndexOptions { Name = "idx_user_visits_user_park_year" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.Status)
                    .Descending(static document => document.UpdatedAt),
                new CreateIndexOptions { Name = "idx_user_visits_user_status_updated" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Descending(static document => document.DateSortKey)
                    .Descending(static document => document.UpdatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "idx_user_visits_user_cursor" }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.CreationOperationKeyHash),
                new CreateIndexOptions<UserVisitDocument>
                {
                    Name = "idx_user_visits_user_creation_operation",
                    Unique = true,
                    PartialFilterExpression = Builders<UserVisitDocument>.Filter.Exists(
                        static document => document.CreationOperationKeyHash,
                        true),
                }),
            PassportAuditMongoDefinitions.BuildPendingMarkerIndex<UserVisitDocument>(
                "idx_user_visits_pending_audit"),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(PurgeJobEnsuredAtUtcPath)
                    .Ascending(PurgeScheduledForUtcPath)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions<UserVisitDocument>
                {
                    Name = "idx_user_visits_pending_purge_schedule",
                    PartialFilterExpression = Builders<UserVisitDocument>.Filter.Exists(
                        DeletedAtUtcPath,
                        true),
                }),
            new CreateIndexModel<UserVisitDocument>(
                Builders<UserVisitDocument>.IndexKeys
                    .Ascending(ExportInvalidationEnsuredAtUtcPath)
                    .Ascending(DeletedAtUtcPath)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions<UserVisitDocument>
                {
                    Name = "idx_user_visits_pending_export_invalidation",
                    PartialFilterExpression = Builders<UserVisitDocument>.Filter.Exists(
                        DeletedAtUtcPath,
                        true),
                }),
        };
    }

    private static string NormalizeRequired(string? value, string parameterName)
    {
        string normalizedValue = value?.Trim() ?? string.Empty;
        if (normalizedValue.Length == 0)
        {
            throw new ArgumentException("A non-empty identifier is required.", parameterName);
        }

        return normalizedValue;
    }
}
