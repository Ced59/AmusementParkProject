using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkGraphUpserts;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

public sealed class ParkGraphUpsertHistoryRepository : IParkGraphUpsertHistoryRepository
{
    private readonly IMongoCollection<ParkGraphUpsertHistoryDocument> collection;

    public ParkGraphUpsertHistoryRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<ParkGraphUpsertHistoryDocument>(settings.ParkGraphUpsertHistoryCollectionName);
    }

    public async Task SaveAsync(ParkGraphUpsertHistoryEntry entry, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(entry);

        string serializedResult = JsonSerializer.Serialize(entry.Result, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        ParkGraphUpsertHistoryDocument document = new ParkGraphUpsertHistoryDocument
        {
            Id = entry.Id,
            OperationKind = entry.OperationKind,
            TargetParkId = entry.TargetParkId,
            TargetParkName = entry.TargetParkName,
            RequestedByUserId = entry.RequestedByUserId,
            RawJson = entry.RawJson,
            Result = BsonDocument.Parse(serializedResult),
            CreatedAt = entry.CreatedAtUtc,
            UpdatedAt = entry.CreatedAtUtc,
        };

        await this.collection.InsertOneAsync(document, cancellationToken: cancellationToken);
    }
}
