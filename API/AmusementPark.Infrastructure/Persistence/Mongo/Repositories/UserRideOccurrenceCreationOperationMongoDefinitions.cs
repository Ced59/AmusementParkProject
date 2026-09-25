using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRideOccurrenceCreationOperationMongoDefinitions
{
    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument> WithContentFence(
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter,
        long? contentFenceToken)
    {
        return !contentFenceToken.HasValue
            ? filter
            : filter & Builders<UserRideOccurrenceCreationOperationDocument>.Filter.Eq(
                static document => document.ContentMutationFenceToken,
                contentFenceToken.Value);
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument> BuildOperationFilter(
        string userId,
        string operationKeyHash)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.OperationKeyHash,
                NormalizeRequired(operationKeyHash, nameof(operationKeyHash)));
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument> BuildOperationFilter(
        UserRideOccurrenceCreationOperationDocument operation)
    {
        ArgumentNullException.ThrowIfNull(operation);
        return WithContentFence(
            BuildOperationFilter(operation.UserId, operation.OperationKeyHash),
            operation.ContentMutationFenceToken);
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildCreationOperationsFilter(
            string userId,
            IReadOnlyCollection<string> operationKeyHashes)
    {
        ArgumentNullException.ThrowIfNull(operationKeyHashes);
        string[] normalizedHashes = operationKeyHashes
            .Select(hash => NormalizeRequired(hash, nameof(operationKeyHashes)))
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.In(
                static document => document.OperationKeyHash,
                normalizedHashes)
            & filters.In(
                static document => document.OperationKind,
                new[] { "creation-key-reservation", "creation" })
            & filters.In(
                static document => document.OperationState,
                new[] { "reserved", "pending", "completed", "conflict" });
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildBatchCreationReleaseFilter(
            string userId,
            string visitId,
            string operationKeyHash)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return BuildOperationFilter(userId, operationKeyHash)
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.In(
                static document => document.OperationKind,
                new[] { "creation-key-reservation", "creation" });
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildBatchCreationReservationReleaseFilter(
            string userId,
            string visitId,
            string operationKeyHash,
            string concurrencyToken)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return BuildOperationFilter(userId, operationKeyHash)
            & filters.Eq(
                static document => document.Id,
                NormalizeRequired(concurrencyToken, nameof(concurrencyToken)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.OperationKind,
                "creation-key-reservation")
            & filters.Eq(
                static document => document.OperationState,
                "reserved");
    }

    public static IReadOnlyCollection<CreateIndexModel<UserRideOccurrenceCreationOperationDocument>>
        BuildIndexes()
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return new[]
        {
            new CreateIndexModel<UserRideOccurrenceCreationOperationDocument>(
                Builders<UserRideOccurrenceCreationOperationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.OperationKeyHash),
                new CreateIndexOptions
                {
                    Name = "idx_user_ride_occurrence_operations_user_key",
                    Unique = true,
                }),
            new CreateIndexModel<UserRideOccurrenceCreationOperationDocument>(
                Builders<UserRideOccurrenceCreationOperationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.VisitId),
                new CreateIndexOptions<UserRideOccurrenceCreationOperationDocument>
                {
                    Name = "idx_user_ride_occurrence_operations_active_order_mutation",
                    Unique = true,
                    PartialFilterExpression = filters.Eq(
                        static document => document.OperationState,
                        "pending"),
                }),
            new CreateIndexModel<UserRideOccurrenceCreationOperationDocument>(
                Builders<UserRideOccurrenceCreationOperationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.RelatedCreationOperationKeyHash)
                    .Ascending(static document => document.VisitId)
                    .Ascending(static document => document.OperationState),
                new CreateIndexOptions<UserRideOccurrenceCreationOperationDocument>
                {
                    Name = "idx_user_ride_occurrence_operations_creation_normalization",
                    PartialFilterExpression = filters.Exists(
                        static document => document.RelatedCreationOperationKeyHash,
                        true),
                }),
            new CreateIndexModel<UserRideOccurrenceCreationOperationDocument>(
                Builders<UserRideOccurrenceCreationOperationDocument>.IndexKeys
                    .Ascending(static document => document.UserId)
                    .Ascending(static document => document.VisitId)
                    .Ascending(static document => document.ContentMutationFenceToken)
                    .Ascending(static document => document.OperationState),
                new CreateIndexOptions
                {
                    Name = "idx_user_ride_occurrence_operations_visit_fence_state",
                }),
            PassportAuditMongoDefinitions
                .BuildPendingMarkerIndex<UserRideOccurrenceCreationOperationDocument>(
                    "idx_user_ride_occurrence_operations_pending_audit"),
        };
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildCompletedCreationNormalizationFilter(
            string userId,
            string visitId,
            string relatedCreationOperationKeyHash,
            long? contentFenceToken = null)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return WithContentFence(filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(
                static document => document.RelatedCreationOperationKeyHash,
                NormalizeRequired(
                    relatedCreationOperationKeyHash,
                    nameof(relatedCreationOperationKeyHash)))
            & filters.Eq(static document => document.OperationKind, "reorder")
            & filters.Eq(static document => document.WasNormalized, true)
            & filters.Eq(static document => document.OperationState, "completed"),
            contentFenceToken);
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildPendingVisitFilter(
            string userId,
            string visitId,
            long? contentFenceToken = null)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return WithContentFence(filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(static document => document.OperationState, "pending"),
            contentFenceToken);
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
