using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRideOccurrenceCreationOperationMongoDefinitions
{
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
            PassportAuditMongoDefinitions
                .BuildPendingMarkerIndex<UserRideOccurrenceCreationOperationDocument>(
                    "idx_user_ride_occurrence_operations_pending_audit"),
        };
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildCompletedCreationNormalizationFilter(
            string userId,
            string visitId,
            string relatedCreationOperationKeyHash)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return filters.Eq(
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
            & filters.Eq(static document => document.OperationState, "completed");
    }

    public static FilterDefinition<UserRideOccurrenceCreationOperationDocument>
        BuildPendingVisitFilter(string userId, string visitId)
    {
        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        return filters.Eq(
                static document => document.UserId,
                NormalizeRequired(userId, nameof(userId)))
            & filters.Eq(
                static document => document.VisitId,
                NormalizeRequired(visitId, nameof(visitId)))
            & filters.Eq(static document => document.OperationState, "pending");
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
