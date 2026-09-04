using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Visits;

[BsonIgnoreExtraElements]
public sealed class PassportExportDocument : MongoDocumentBase
{
    [BsonElement("userId")]
    public string UserId { get; set; } = string.Empty;

    [BsonElement("format")]
    [BsonRepresentation(BsonType.String)]
    public PassportExportFormat Format { get; set; }

    [BsonElement("status")]
    [BsonRepresentation(BsonType.String)]
    public PassportExportStatus Status { get; set; }

    [BsonElement("schemaVersion")]
    public int SchemaVersion { get; set; }

    [BsonElement("expiresAtUtc")]
    public DateTime ExpiresAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("fileName")]
    [BsonIgnoreIfNull]
    public string? FileName { get; set; }

    [BsonElement("contentType")]
    [BsonIgnoreIfNull]
    public string? ContentType { get; set; }

    [BsonElement("sizeBytes")]
    [BsonIgnoreIfNull]
    public long? SizeBytes { get; set; }

    [BsonElement("chunkCount")]
    [BsonIgnoreIfNull]
    public int? ChunkCount { get; set; }

    [BsonElement("generationId")]
    [BsonIgnoreIfNull]
    public string? GenerationId { get; set; }

    [BsonElement("checksumSha256")]
    [BsonIgnoreIfNull]
    public string? ChecksumSha256 { get; set; }

    [BsonElement("errorCode")]
    [BsonIgnoreIfNull]
    public string? ErrorCode { get; set; }
}
