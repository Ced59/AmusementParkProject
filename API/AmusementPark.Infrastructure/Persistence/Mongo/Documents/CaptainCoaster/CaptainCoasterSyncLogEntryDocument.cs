using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterSyncLogEntryDocument
{
    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("level")]
    public string Level { get; set; } = "Info";

    [BsonElement("message")]
    public string Message { get; set; } = string.Empty;
}
