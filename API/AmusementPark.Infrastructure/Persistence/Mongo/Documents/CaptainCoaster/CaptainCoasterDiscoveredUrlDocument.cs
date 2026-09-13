using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterDiscoveredUrlDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("captainCoasterId")]
    public string CaptainCoasterId { get; set; } = string.Empty;

    [BsonElement("language")]
    public string Language { get; set; } = "fr";

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("sequence")]
    public int Sequence { get; set; }

    [BsonElement("discoveredAtUtc")]
    public DateTime DiscoveredAtUtc { get; set; } = DateTime.UtcNow;
}
