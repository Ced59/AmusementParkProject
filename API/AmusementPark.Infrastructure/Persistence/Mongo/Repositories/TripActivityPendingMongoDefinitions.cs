using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Trips;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class TripActivityPendingMongoDefinitions
{
    public static UpdateDefinition<TDocument> Append<TDocument>(
        UpdateDefinition<TDocument> update,
        TripActivityWrite? activity)
    {
        ArgumentNullException.ThrowIfNull(update);
        if (activity is null)
        {
            return update;
        }

        TripActivityPendingDocument pending = TripActivityPendingDocument.FromWrite(activity);
        BsonDocument pendingBson = pending.ToBsonDocument(
            BsonSerializer.LookupSerializer<TripActivityPendingDocument>());
        UpdateDefinition<TDocument> push = new BsonDocumentUpdateDefinition<TDocument>(
            new BsonDocument("$push", new BsonDocument("pendingAuditEvents", pendingBson)));
        return Builders<TDocument>.Update.Combine(update, push);
    }

    public static List<TripActivityPendingDocument> CreateList(TripActivityWrite? activity)
    {
        return activity is null
            ? new List<TripActivityPendingDocument>()
            : new List<TripActivityPendingDocument>
            {
                TripActivityPendingDocument.FromWrite(activity),
            };
    }

    public static CreateIndexModel<TDocument> BuildPendingIndex<TDocument>(string name)
    {
        return new CreateIndexModel<TDocument>(
            Builders<TDocument>.IndexKeys.Ascending("pendingAuditEvents.operationKey"),
            new CreateIndexOptions
            {
                Name = name,
                Sparse = true,
            });
    }
}
