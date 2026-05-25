using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkGraphUpserts;

public sealed class ParkGraphUpsertHistoryDocument : MongoDocumentBase
{
    [BsonElement("operationKind")]
    public string OperationKind { get; set; } = "preview";

    [BsonElement("targetParkId")]
    [BsonIgnoreIfNull]
    public string? TargetParkId { get; set; }

    [BsonElement("targetParkName")]
    [BsonIgnoreIfNull]
    public string? TargetParkName { get; set; }

    [BsonElement("requestedByUserId")]
    [BsonIgnoreIfNull]
    public string? RequestedByUserId { get; set; }

    [BsonElement("rawJson")]
    public string RawJson { get; set; } = string.Empty;

    [BsonElement("result")]
    public BsonDocument Result { get; set; } = new BsonDocument();
}
