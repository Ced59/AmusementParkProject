using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal enum RideOccurrenceOrderGuardValidationStatus
{
    Validated = 1,
    Stale = 2,
    Unavailable = 3,
}

internal sealed class UserRideOccurrenceOrderGuardValidator
{
    private const string PendingOperationState = "pending";
    private const string CompletedOperationState = "completed";

    private readonly IMongoCollection<UserRideOccurrenceDocument> collection;
    private readonly IMongoCollection<UserRideOccurrenceCreationOperationDocument>
        operationCollection;

    public UserRideOccurrenceOrderGuardValidator(
        IMongoCollection<UserRideOccurrenceDocument> collection,
        IMongoCollection<UserRideOccurrenceCreationOperationDocument> operationCollection)
    {
        ArgumentNullException.ThrowIfNull(collection);
        ArgumentNullException.ThrowIfNull(operationCollection);
        this.collection = collection;
        this.operationCollection = operationCollection;
    }

    public async Task<RideOccurrenceOrderGuardValidationStatus> EnsureValidatedAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        RideOccurrenceReorderRequest request,
        CancellationToken cancellationToken)
    {
        if (operation.OrderGuardsValidated)
        {
            return RideOccurrenceOrderGuardValidationStatus.Validated;
        }

        IReadOnlyCollection<UserRideOccurrenceOrderGuardDocument> guards =
            operation.OrderGuards is null
                ? Array.Empty<UserRideOccurrenceOrderGuardDocument>()
                : operation.OrderGuards;
        List<UserRideOccurrenceDocument> current = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildActiveVisitFilter(
                request.VisitId.Value,
                request.UserId))
            .Limit(RideOccurrenceOrderPlanner.MaximumReorderSize + 1)
            .ToListAsync(cancellationToken);
        if (!GuardsMatch(guards, current))
        {
            UserRideOccurrenceCreationOperationDocument? concurrent =
                await this.LoadOperationAsync(operation, cancellationToken);
            if (concurrent is not null)
            {
                operation.OperationState = concurrent.OperationState;
                if (concurrent.OrderGuardsValidated
                    && concurrent.OperationState is (
                        PendingOperationState or CompletedOperationState))
                {
                    operation.OrderGuardsValidated = true;
                    return RideOccurrenceOrderGuardValidationStatus.Validated;
                }
            }

            return concurrent is not null
                && string.Equals(
                    concurrent.OperationState,
                    PendingOperationState,
                    StringComparison.Ordinal)
                ? RideOccurrenceOrderGuardValidationStatus.Stale
                : RideOccurrenceOrderGuardValidationStatus.Unavailable;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Eq(static document => document.OrderGuardsValidated, false);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            Builders<UserRideOccurrenceCreationOperationDocument>.Update.Set(
                static document => document.OrderGuardsValidated,
                true),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount != 1)
        {
            UserRideOccurrenceCreationOperationDocument? concurrent =
                await this.LoadOperationAsync(operation, cancellationToken);
            if (concurrent is not null)
            {
                operation.OperationState = concurrent.OperationState;
                if (concurrent.OrderGuardsValidated
                    && concurrent.OperationState is (
                        PendingOperationState or CompletedOperationState))
                {
                    operation.OrderGuardsValidated = true;
                    return RideOccurrenceOrderGuardValidationStatus.Validated;
                }
            }

            return RideOccurrenceOrderGuardValidationStatus.Unavailable;
        }

        operation.OrderGuardsValidated = true;
        return RideOccurrenceOrderGuardValidationStatus.Validated;
    }

    public async Task<RideOccurrenceOrderGuardValidationStatus>
        EnsureAppendBaseValidatedAsync(
            UserRideOccurrenceCreationOperationDocument operation,
            CancellationToken cancellationToken)
    {
        if (operation.AppendBaseValidated)
        {
            return RideOccurrenceOrderGuardValidationStatus.Validated;
        }

        UserRideOccurrenceDocument? last = await this.collection
            .Find(UserRideOccurrenceMongoDefinitions.BuildActiveVisitFilter(
                operation.VisitId!,
                operation.UserId))
            .Sort(UserRideOccurrenceMongoDefinitions.BuildReverseVisitOrderSort())
            .Limit(1)
            .FirstOrDefaultAsync(cancellationToken);
        bool matches = operation.AppendBaseWasEmpty
            ? last is null
            : last?.SortPosition == operation.AppendBaseSortPosition;
        if (!matches)
        {
            return RideOccurrenceOrderGuardValidationStatus.Stale;
        }

        FilterDefinitionBuilder<UserRideOccurrenceCreationOperationDocument> filters =
            Builders<UserRideOccurrenceCreationOperationDocument>.Filter;
        FilterDefinition<UserRideOccurrenceCreationOperationDocument> filter =
            UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash)
            & filters.Eq(static document => document.OperationState, PendingOperationState)
            & filters.Eq(static document => document.AppendBaseValidated, false);
        UpdateResult result = await this.operationCollection.UpdateOneAsync(
            filter,
            Builders<UserRideOccurrenceCreationOperationDocument>.Update.Set(
                static document => document.AppendBaseValidated,
                true),
            new UpdateOptions { IsUpsert = false },
            cancellationToken);
        if (result.MatchedCount != 1)
        {
            UserRideOccurrenceCreationOperationDocument? concurrent =
                await this.LoadOperationAsync(operation, cancellationToken);
            return concurrent?.AppendBaseValidated == true
                ? RideOccurrenceOrderGuardValidationStatus.Validated
                : RideOccurrenceOrderGuardValidationStatus.Unavailable;
        }

        operation.AppendBaseValidated = true;
        return RideOccurrenceOrderGuardValidationStatus.Validated;
    }

    internal static bool GuardsMatch(
        IReadOnlyCollection<UserRideOccurrenceOrderGuardDocument> guards,
        IReadOnlyCollection<UserRideOccurrenceDocument> current)
    {
        ArgumentNullException.ThrowIfNull(guards);
        ArgumentNullException.ThrowIfNull(current);
        IReadOnlyDictionary<string, UserRideOccurrenceDocument> byId = current
            .ToDictionary(static document => document.Id, StringComparer.Ordinal);
        return current.Count == guards.Count
            && guards.All(guard =>
                byId.TryGetValue(
                    guard.OccurrenceId,
                    out UserRideOccurrenceDocument? document)
                && document.SortPosition == guard.SortPosition);
    }

    private async Task<UserRideOccurrenceCreationOperationDocument?> LoadOperationAsync(
        UserRideOccurrenceCreationOperationDocument operation,
        CancellationToken cancellationToken)
    {
        return await this.operationCollection
            .Find(UserRideOccurrenceCreationOperationMongoDefinitions.BuildOperationFilter(
                operation.UserId,
                operation.OperationKeyHash))
            .FirstOrDefaultAsync(cancellationToken);
    }
}
