using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class PassportExportChunkDocument : MongoDocumentBase
{
    [BsonElement("exportId")]
    public string ExportId { get; set; } = string.Empty;

    [BsonElement("generationId")]
    public string GenerationId { get; set; } = string.Empty;

    [BsonElement("index")]
    public int Index { get; set; }

    [BsonElement("data")]
    public byte[] Data { get; set; } = Array.Empty<byte>();

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }
}
