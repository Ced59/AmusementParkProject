using AmusementPark.Core.Domain.Trips;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripChildMutationMongoDefinitions
{
    public static FilterDefinition<TDocument> BuildCreationLeaseGuard<TDocument>()
    {
        return new BsonDocumentFilterDefinition<TDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$lt", new BsonArray { "$$NOW", "$leaseExpiresAtUtc" })));
    }

    public static FilterDefinition<TDocument> BuildExpiredCreationLeaseGuard<TDocument>()
    {
        return new BsonDocumentFilterDefinition<TDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$lte", new BsonArray
            {
                "$leaseExpiresAtUtc",
                "$$NOW",
            })));
    }

    public static FilterDefinition<TDocument> BuildPendingLeaseGuard<TDocument>()
    {
        return new BsonDocumentFilterDefinition<TDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$lt", new BsonArray
            {
                "$$NOW",
                "$pendingMutation.leaseExpiresAtUtc",
            })));
    }

    public static FilterDefinition<TDocument> BuildPendingAvailableGuard<TDocument>()
    {
        return new BsonDocumentFilterDefinition<TDocument>(new BsonDocument(
            "$expr",
            new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("$eq", new BsonArray
                {
                    new BsonDocument("$type", "$pendingMutation"),
                    "missing",
                }),
                new BsonDocument("$lte", new BsonArray
                {
                    "$pendingMutation.leaseExpiresAtUtc",
                    "$$NOW",
                }),
            })));
    }

    public static PendingTripChildMutationDocument ToPendingDocument(TripChildMutationLease lease)
    {
        ArgumentNullException.ThrowIfNull(lease);
        return new PendingTripChildMutationDocument
        {
            OperationId = lease.OperationId,
            ChildMutationEpoch = lease.ChildMutationEpoch,
            LeaseGeneration = lease.Generation,
            LeaseExpiresAtUtc = lease.ExpiresAtUtc,
        };
    }

    public static bool HasSameIdentity(
        PendingTripChildMutationDocument? pending,
        TripChildMutationLease lease)
    {
        return pending is not null
            && string.Equals(pending.OperationId, lease.OperationId, StringComparison.Ordinal)
            && pending.ChildMutationEpoch == lease.ChildMutationEpoch
            && pending.LeaseGeneration == lease.Generation
            && pending.LeaseExpiresAtUtc == lease.ExpiresAtUtc;
    }
}
