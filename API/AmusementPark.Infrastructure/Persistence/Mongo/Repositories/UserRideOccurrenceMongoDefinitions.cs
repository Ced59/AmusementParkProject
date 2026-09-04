using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRideOccurrenceMongoDefinitions
{
    public static FilterDefinition<UserRideOccurrenceDocument> WithContentFence(
        FilterDefinition<UserRideOccurrenceDocument> filter,
        long? contentFenceToken)
    {
        return !contentFenceToken.HasValue
            ? filter
            : filter & Builders<UserRideOccurrenceDocument>.Filter.Eq(
                static document => document.ContentMutationFenceToken,
                contentFenceToken.Value);
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildOwnedOccurrenceByIdFilter(
        string occurrenceId,
        string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.Id,
                NormalizeRequired(occurrenceId, nameof(occurrenceId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(static document => document.DeletedAtUtc, null);
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildOwnedOccurrenceFilter(
        string occurrenceId,
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.Id,
                NormalizeRequired(occurrenceId, nameof(occurrenceId)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(static document => document.DeletedAtUtc, null);
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildOwnedAnyStateFilter(
        string occurrenceId,
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.Id,
                NormalizeRequired(occurrenceId, nameof(occurrenceId)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)));
    }

    public static FilterDefinition<UserRideOccurrenceDocument>
        BuildOwnedAnyStateReorderVersionFilter(
            string occurrenceId,
            string visitId,
            string userId,
            long expectedVersion,
            string operationKeyHash)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return BuildOwnedAnyStateFilter(occurrenceId, visitId, userId)
            & filters.Eq(static document => document.Version, expectedVersion)
            & filters.Eq(
                static document => document.LastReorderOperationKeyHash,
                NormalizeRequired(operationKeyHash, nameof(operationKeyHash)));
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildOwnedVersionFilter(
        string occurrenceId,
        string visitId,
        string userId,
        long expectedVersion)
    {
        if (expectedVersion < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(expectedVersion),
                "The expected ride occurrence version must be positive.");
        }

        return BuildOwnedOccurrenceFilter(occurrenceId, visitId, userId)
            & Builders<UserRideOccurrenceDocument>.Filter.Eq(
                static document => document.Version,
                expectedVersion);
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildCreationOperationFilter(
        string userId,
        string operationKeyHash)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.CreationOperationKeyHash,
                NormalizeRequired(operationKeyHash, nameof(operationKeyHash)));
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildListFilter(
        RideOccurrenceListCriteria criteria)
    {
        ArgumentNullException.ThrowIfNull(criteria);

        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        FilterDefinition<UserRideOccurrenceDocument> filter = filters.Eq(
                static document => document.VisitId,
                criteria.VisitId.Value)
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(criteria.UserId, nameof(criteria.UserId)))
            & filters.Eq(static document => document.DeletedAtUtc, null);
        if (criteria.After is not null)
        {
            RideOccurrenceListCursor cursor = criteria.After;
            FilterDefinition<UserRideOccurrenceDocument> afterFilter =
                filters.Gt(static document => document.SortPosition, cursor.SortPosition)
                | (filters.Eq(static document => document.SortPosition, cursor.SortPosition)
                    & filters.Gt(static document => document.CreatedAt, cursor.CreatedAtUtc))
                | (filters.Eq(static document => document.SortPosition, cursor.SortPosition)
                    & filters.Eq(static document => document.CreatedAt, cursor.CreatedAtUtc)
                    & filters.Gt(static document => document.Id, cursor.OccurrenceId.Value));
            filter &= afterFilter;
        }

        return filter;
    }

    public static FilterDefinition<UserRideOccurrenceDocument> BuildActiveVisitFilter(
        string visitId,
        string userId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceDocument> filters =
            Builders<UserRideOccurrenceDocument>.Filter;
        return filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(static document => document.DeletedAtUtc, null);
    }

    public static SortDefinition<UserRideOccurrenceDocument> BuildVisitOrderSort()
    {
        return Builders<UserRideOccurrenceDocument>.Sort
            .Ascending(static document => document.SortPosition)
            .Ascending(static document => document.CreatedAt)
            .Ascending(static document => document.Id);
    }

    public static SortDefinition<UserRideOccurrenceDocument> BuildReverseVisitOrderSort()
    {
        return Builders<UserRideOccurrenceDocument>.Sort
            .Descending(static document => document.SortPosition)
            .Descending(static document => document.CreatedAt)
            .Descending(static document => document.Id);
    }

    public static SortDefinition<UserRideOccurrenceDocument> BuildCreationOperationSort()
    {
        return Builders<UserRideOccurrenceDocument>.Sort
            .Ascending(static document => document.CreationOperationIndex);
    }

    public static IReadOnlyCollection<CreateIndexModel<UserRideOccurrenceDocument>> BuildIndexes()
    {
        return new List<CreateIndexModel<UserRideOccurrenceDocument>>
        {
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.VisitId)
                    .Ascending(static document => document.SortPosition)
                    .Ascending(static document => document.CreatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions { Name = "idx_user_ride_occurrences_visit_order" }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.VisitId)
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ContentMutationFenceToken)
                    .Ascending(static document => document.SortPosition)
                    .Ascending(static document => document.CreatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions
                {
                    Name = "idx_user_ride_occurrences_visit_fenced_order",
                }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkItemId)
                    .Ascending(static document => document.VisitId),
                new CreateIndexOptions { Name = "idx_user_ride_occurrences_user_item_visit" }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.ParkId)
                    .Ascending(static document => document.VisitId),
                new CreateIndexOptions { Name = "idx_user_ride_occurrences_user_park_visit" }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.VisitId)
                    .Ascending(static document => document.Status),
                new CreateIndexOptions { Name = "idx_user_ride_occurrences_visit_status" }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.DeletedAtUtc),
                new CreateIndexOptions { Name = "idx_user_ride_occurrences_user_deleted" }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.CreationOperationKeyHash)
                    .Ascending(static document => document.CreationOperationIndex),
                new CreateIndexOptions<UserRideOccurrenceDocument>
                {
                    Name = "idx_user_ride_occurrences_user_creation_operation_item",
                    Unique = true,
                    PartialFilterExpression = Builders<UserRideOccurrenceDocument>.Filter.Exists(
                        static document => document.CreationOperationKeyHash,
                        true),
                }),
            new CreateIndexModel<UserRideOccurrenceDocument>(
                Builders<UserRideOccurrenceDocument>.IndexKeys
                    .Ascending(static document => document.CreationPendingCompletion)
                    .Ascending(static document => document.CreatedAt)
                    .Ascending(static document => document.Id),
                new CreateIndexOptions<UserRideOccurrenceDocument>
                {
                    Name = "idx_user_ride_occurrences_pending_creation_completion",
                    PartialFilterExpression = Builders<UserRideOccurrenceDocument>.Filter.Eq(
                        static document => document.CreationPendingCompletion,
                        true),
                }),
            PassportAuditMongoDefinitions.BuildPendingMarkerIndex<UserRideOccurrenceDocument>(
                "idx_user_ride_occurrences_pending_audit"),
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
