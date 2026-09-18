using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class TripChildMutationLeaseRepository : ITripChildMutationLeaseRepository
{
    public const int MaximumActiveLeases = 1;
    public static readonly TimeSpan LeaseDuration = TimeSpan.FromSeconds(20);

    private readonly IMongoCollection<TripPlanDocument> collection;

    public TripChildMutationLeaseRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        ArgumentNullException.ThrowIfNull(database);
        ArgumentNullException.ThrowIfNull(settings);
        this.collection = database.GetCollection<TripPlanDocument>(settings.TripPlansCollectionName);
    }

    internal TripChildMutationLeaseRepository(IMongoCollection<TripPlanDocument> collection)
    {
        this.collection = collection ?? throw new ArgumentNullException(nameof(collection));
    }

    public async Task<TripChildMutationLease?> TryAcquireOwnedAsync(
        TripPlanId tripPlanId,
        string ownerUserId,
        TripMemberId actorMemberId,
        long expectedPlanVersion,
        long childMutationEpoch,
        string operationId,
        CancellationToken cancellationToken)
    {
        string normalizedOperationId = NormalizeOperationId(operationId);
        FilterDefinition<TripPlanDocument> identityFilter = BuildIdentityFilter(
            tripPlanId,
            ownerUserId,
            actorMemberId,
            expectedPlanVersion,
            childMutationEpoch);
        BsonDocument activeLeaseFilter = new("$expr", new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$lt", new BsonArray
            {
                BuildActiveLeaseCountExpression(null),
                MaximumActiveLeases,
            }),
            new BsonDocument("$eq", new BsonArray
            {
                BuildActiveLeaseCountExpression(normalizedOperationId),
                0,
            }),
        }));
        FilterDefinition<TripPlanDocument> acquireFilter = identityFilter
            & new BsonDocumentFilterDefinition<TripPlanDocument>(activeLeaseFilter);
        BsonDocument activeLeases = BuildActiveLeasesExpression();
        BsonDocument nextGeneration = new("$add", new BsonArray
        {
            new BsonDocument("$ifNull", new BsonArray { "$childMutationLeaseSequence", 0 }),
            1,
        });
        BsonDocument newLease = new()
        {
            { "operationId", normalizedOperationId },
            { "actorMemberId", actorMemberId.Value },
            { "childMutationEpoch", childMutationEpoch },
            { "generation", nextGeneration },
            {
                "expiresAtUtc",
                new BsonDocument("$dateAdd", new BsonDocument
                {
                    { "startDate", "$$NOW" },
                    { "unit", "second" },
                    { "amount", (int)LeaseDuration.TotalSeconds },
                })
            },
        };
        BsonDocument updateStage = new("$set", new BsonDocument
        {
            {
                "activeChildMutationLeases",
                new BsonDocument("$concatArrays", new BsonArray { activeLeases, new BsonArray { newLease } })
            },
            { "childMutationLeaseSequence", nextGeneration },
        });
        PipelineUpdateDefinition<TripPlanDocument> update = new(new[] { updateStage });
        FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument> options = new()
        {
            ReturnDocument = ReturnDocument.After,
        };
        TripPlanDocument? updated = await this.collection.FindOneAndUpdateAsync(
            acquireFilter,
            update,
            options,
            cancellationToken);
        TripChildMutationLeaseDocument? acquired = updated?.ActiveChildMutationLeases.SingleOrDefault(
            lease => string.Equals(lease.OperationId, normalizedOperationId, StringComparison.Ordinal));
        return acquired is null ? null : ToDomain(acquired);
    }

    public async Task<TripChildMutationLease?> TryAcquireAccessibleAsync(
        TripPlanId tripPlanId,
        string actorUserId,
        TripMemberId actorMemberId,
        long expectedPlanVersion,
        long childMutationEpoch,
        string operationId,
        CancellationToken cancellationToken)
    {
        string normalizedOperationId = NormalizeOperationId(operationId);
        FilterDefinition<TripPlanDocument> identityFilter = BuildAccessibleIdentityFilter(
            tripPlanId,
            actorUserId,
            actorMemberId,
            expectedPlanVersion,
            childMutationEpoch);
        BsonDocument activeLeaseFilter = new("$expr", new BsonDocument("$and", new BsonArray
        {
            new BsonDocument("$lt", new BsonArray
            {
                BuildActiveLeaseCountExpression(null),
                MaximumActiveLeases,
            }),
            new BsonDocument("$eq", new BsonArray
            {
                BuildActiveLeaseCountExpression(normalizedOperationId),
                0,
            }),
        }));
        FilterDefinition<TripPlanDocument> acquireFilter = identityFilter
            & new BsonDocumentFilterDefinition<TripPlanDocument>(activeLeaseFilter);
        BsonDocument activeLeases = BuildActiveLeasesExpression();
        BsonDocument nextGeneration = new("$add", new BsonArray
        {
            new BsonDocument("$ifNull", new BsonArray { "$childMutationLeaseSequence", 0 }),
            1,
        });
        BsonDocument newLease = new()
        {
            { "operationId", normalizedOperationId },
            { "actorMemberId", actorMemberId.Value },
            { "childMutationEpoch", childMutationEpoch },
            { "generation", nextGeneration },
            {
                "expiresAtUtc",
                new BsonDocument("$dateAdd", new BsonDocument
                {
                    { "startDate", "$$NOW" },
                    { "unit", "second" },
                    { "amount", (int)LeaseDuration.TotalSeconds },
                })
            },
        };
        PipelineUpdateDefinition<TripPlanDocument> update = new(new[]
        {
            new BsonDocument("$set", new BsonDocument
            {
                {
                    "activeChildMutationLeases",
                    new BsonDocument("$concatArrays", new BsonArray
                    {
                        activeLeases,
                        new BsonArray { newLease },
                    })
                },
                { "childMutationLeaseSequence", nextGeneration },
            }),
        });
        TripPlanDocument? updated = await this.collection.FindOneAndUpdateAsync(
            acquireFilter,
            update,
            new FindOneAndUpdateOptions<TripPlanDocument, TripPlanDocument>
            {
                ReturnDocument = ReturnDocument.After,
            },
            cancellationToken);
        TripChildMutationLeaseDocument? acquired = updated?.ActiveChildMutationLeases.SingleOrDefault(
            lease => string.Equals(lease.OperationId, normalizedOperationId, StringComparison.Ordinal));
        return acquired is null ? null : ToDomain(acquired);
    }

    public Task ReleaseAsync(
        TripPlanId tripPlanId,
        TripChildMutationLease lease,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(lease);
        FilterDefinition<TripPlanDocument> filter = Builders<TripPlanDocument>.Filter.Eq(
            static document => document.Id,
            tripPlanId.Value);
        UpdateDefinition<TripPlanDocument> update = Builders<TripPlanDocument>.Update.PullFilter(
            static document => document.ActiveChildMutationLeases,
            item => item.OperationId == lease.OperationId
                && item.ChildMutationEpoch == lease.ChildMutationEpoch
                && item.Generation == lease.Generation);
        return this.collection.UpdateOneAsync(filter, update, cancellationToken: cancellationToken);
    }

    private static FilterDefinition<TripPlanDocument> BuildIdentityFilter(
        TripPlanId tripPlanId,
        string ownerUserId,
        TripMemberId actorMemberId,
        long expectedPlanVersion,
        long childMutationEpoch)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> epochFilter = childMutationEpoch == 1
            ? filters.Eq(static document => document.ChildMutationEpoch, 1)
                | filters.Exists(static document => document.ChildMutationEpoch, false)
            : filters.Eq(static document => document.ChildMutationEpoch, childMutationEpoch);
        return filters.Eq(static document => document.Id, tripPlanId.Value)
            & filters.Eq(static document => document.OwnerUserId, ownerUserId)
            & filters.Eq(static document => document.Version, expectedPlanVersion)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None)
            & epochFilter
            & filters.ElemMatch(
                static document => document.Members,
                member => member.MemberId == actorMemberId.Value
                    && member.UserId == ownerUserId
                    && member.State == TripMembershipState.Active);
    }

    private static FilterDefinition<TripPlanDocument> BuildAccessibleIdentityFilter(
        TripPlanId tripPlanId,
        string actorUserId,
        TripMemberId actorMemberId,
        long expectedPlanVersion,
        long childMutationEpoch)
    {
        FilterDefinitionBuilder<TripPlanDocument> filters = Builders<TripPlanDocument>.Filter;
        FilterDefinition<TripPlanDocument> epochFilter = childMutationEpoch == 1
            ? filters.Eq(static document => document.ChildMutationEpoch, 1)
                | filters.Exists(static document => document.ChildMutationEpoch, false)
            : filters.Eq(static document => document.ChildMutationEpoch, childMutationEpoch);
        return filters.Eq(static document => document.Id, tripPlanId.Value)
            & filters.Eq(static document => document.Version, expectedPlanVersion)
            & filters.Eq(static document => document.DeletionState, TripDeletionState.None)
            & epochFilter
            & filters.ElemMatch(
                static document => document.Members,
                member => member.MemberId == actorMemberId.Value
                    && member.UserId == actorUserId
                    && member.State == TripMembershipState.Active);
    }

    private static BsonDocument BuildActiveLeasesExpression()
    {
        return new BsonDocument("$filter", new BsonDocument
        {
            {
                "input",
                new BsonDocument("$ifNull", new BsonArray { "$activeChildMutationLeases", new BsonArray() })
            },
            { "as", "lease" },
            { "cond", new BsonDocument("$gt", new BsonArray { "$$lease.expiresAtUtc", "$$NOW" }) },
        });
    }

    private static BsonDocument BuildActiveLeaseCountExpression(string? operationId)
    {
        BsonDocument input = BuildActiveLeasesExpression();
        if (operationId is not null)
        {
            input = new BsonDocument("$filter", new BsonDocument
            {
                { "input", input },
                { "as", "lease" },
                { "cond", new BsonDocument("$eq", new BsonArray { "$$lease.operationId", operationId }) },
            });
        }

        return new BsonDocument("$size", input);
    }

    private static TripChildMutationLease ToDomain(TripChildMutationLeaseDocument document)
    {
        return new TripChildMutationLease(
            document.OperationId,
            TripMemberId.Parse(document.ActorMemberId),
            document.ChildMutationEpoch,
            document.Generation,
            DateTime.SpecifyKind(document.ExpiresAtUtc, DateTimeKind.Utc));
    }

    private static string NormalizeOperationId(string? value)
    {
        string normalized = value?.Trim() ?? string.Empty;
        if (normalized.Length is 0 or > 100)
        {
            throw new ArgumentException("A bounded child operation identifier is required.", nameof(value));
        }

        return normalized;
    }
}
