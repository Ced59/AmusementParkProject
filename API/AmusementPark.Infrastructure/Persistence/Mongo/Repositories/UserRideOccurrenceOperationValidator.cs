using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class UserRideOccurrenceOperationValidator
{
    private const string CreationOperationKind = "creation";
    private const string ReorderOperationKind = "reorder";
    private const string PendingOperationState = "pending";
    private const string CompletedOperationState = "completed";
    private const string ConflictOperationState = "conflict";

    public static bool CreationMatches(
        UserRideOccurrenceCreationOperationDocument operation,
        string payloadHash,
        int expectedCount)
    {
        if (expectedCount is < 1 or > UserRideOccurrenceRepository.MaximumBatchSize
            || !string.Equals(
                operation.OperationKind,
                CreationOperationKind,
                StringComparison.Ordinal)
            || string.IsNullOrWhiteSpace(operation.VisitId)
            || operation.OperationState is not (PendingOperationState or CompletedOperationState)
            || operation.AppendBaseWasEmpty == operation.AppendBaseSortPosition.HasValue
            || !string.Equals(operation.PayloadHash, payloadHash, StringComparison.Ordinal)
            || operation.Items.Count != expectedCount)
        {
            return false;
        }

        int distinctIndexes = operation.Items
            .Select(static item => item.Index)
            .Distinct()
            .Count();
        int distinctIds = operation.Items
            .Select(static item => item.OccurrenceId)
            .Distinct(StringComparer.Ordinal)
            .Count();
        int distinctPositions = operation.Items
            .Select(static item => item.SortPosition)
            .Distinct()
            .Count();
        return distinctIndexes == expectedCount
            && distinctIds == expectedCount
            && distinctPositions == expectedCount
            && operation.Items.All(item =>
                item.Index is >= 0
                && item.Index < expectedCount
                && !string.IsNullOrWhiteSpace(item.OccurrenceId)
                && item.CreatedAtUtc.Kind == DateTimeKind.Utc
                && item.UpdatedAtUtc.Kind == DateTimeKind.Utc
                && item.UpdatedAtUtc >= item.CreatedAtUtc
                && item.CreationSnapshot is not null
                && string.Equals(
                    item.CreationSnapshot.VisitId,
                    operation.VisitId,
                    StringComparison.Ordinal)
                && item.CreationSnapshot.SortPosition == item.SortPosition
                && item.CreationSnapshot.CreatedAtUtc == item.CreatedAtUtc
                && item.CreationSnapshot.UpdatedAtUtc == item.UpdatedAtUtc);
    }

    public static bool ReorderMatches(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        string payloadHash)
    {
        bool requiresAnchor = request.Placement is RideOccurrencePlacement.Before
            or RideOccurrencePlacement.After;
        return requiresAnchor == request.AnchorOccurrenceId.HasValue
            && request.AnchorOccurrenceId != request.OccurrenceId
            && operation.OperationState is PendingOperationState
                or CompletedOperationState
                or ConflictOperationState
            && string.Equals(operation.OperationKind, ReorderOperationKind, StringComparison.Ordinal)
            && string.Equals(operation.PayloadHash, payloadHash, StringComparison.Ordinal)
            && string.Equals(operation.VisitId, request.VisitId.Value, StringComparison.Ordinal)
            && string.Equals(operation.UserId, request.UserId, StringComparison.Ordinal)
            && string.Equals(
                operation.MovedOccurrenceId,
                request.OccurrenceId.Value,
                StringComparison.Ordinal)
            && operation.ReorderExpectedVersion == request.ExpectedVersion
            && string.Equals(
                operation.ReorderAnchorOccurrenceId,
                request.AnchorOccurrenceId?.Value,
                StringComparison.Ordinal)
            && operation.ReorderPlacement == request.Placement
            && operation.ReorderItems is not null
            && operation.ReorderItems.Count <= RideOccurrenceOrderPlanner.MaximumReorderSize
            && operation.ReorderItems.Select(static item => item.Index).Distinct().Count()
                == operation.ReorderItems.Count
            && operation.ReorderItems.Select(static item => item.OccurrenceId)
                .Distinct(StringComparer.Ordinal).Count() == operation.ReorderItems.Count
            && operation.ReorderItems.All(item =>
                item.Index is >= 0
                && item.Index < operation.ReorderItems.Count
                && !string.IsNullOrWhiteSpace(item.OccurrenceId)
                && item.ExpectedVersion >= 1
                && item.ResultVersion == item.ExpectedVersion + 1
                && item.ResultSortPosition != item.PreviousSortPosition
                && item.ResultUpdatedAtUtc.Kind == DateTimeKind.Utc)
            && operation.OrderGuards is not null
            && operation.OrderGuards.Count is >= 1
                and <= RideOccurrenceOrderPlanner.MaximumReorderSize
            && operation.OrderGuards.All(
                static guard => !string.IsNullOrWhiteSpace(guard.OccurrenceId))
            && operation.OrderGuards.Select(static guard => guard.OccurrenceId)
                .Distinct(StringComparer.Ordinal).Count() == operation.OrderGuards.Count
            && operation.OrderGuards.Any(guard => string.Equals(
                guard.OccurrenceId,
                request.OccurrenceId.Value,
                StringComparison.Ordinal))
            && operation.ReorderResultSnapshot is not null
            && string.Equals(
                operation.ReorderResultSnapshot.VisitId,
                request.VisitId.Value,
                StringComparison.Ordinal)
            && (operation.ReorderItems.Any(item => string.Equals(
                    item.OccurrenceId,
                    request.OccurrenceId.Value,
                    StringComparison.Ordinal))
                || operation.ReorderResultSnapshot.Version == request.ExpectedVersion);
    }
}
