using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.AdminAudit;

/// <summary>
/// Document Mongo d'une action d'administration auditée.
/// </summary>
public sealed class AdminAuditLogDocument : MongoDocumentBase
{
    [BsonElement("occurredAtUtc")]
    public DateTime OccurredAtUtc { get; set; } = DateTime.UtcNow;

    [BsonElement("action")]
    public string Action { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("entityId")]
    [BsonIgnoreIfNull]
    public string? EntityId { get; set; }

    [BsonElement("actorUserId")]
    [BsonIgnoreIfNull]
    public string? ActorUserId { get; set; }

    [BsonElement("actorEmail")]
    [BsonIgnoreIfNull]
    public string? ActorEmail { get; set; }

    [BsonElement("actorRoles")]
    public List<string> ActorRoles { get; set; } = new List<string>();

    [BsonElement("httpMethod")]
    public string HttpMethod { get; set; } = string.Empty;

    [BsonElement("path")]
    public string Path { get; set; } = string.Empty;

    [BsonElement("statusCode")]
    public int StatusCode { get; set; }

    [BsonElement("ipAddress")]
    [BsonIgnoreIfNull]
    public string? IpAddress { get; set; }

    [BsonElement("userAgent")]
    [BsonIgnoreIfNull]
    public string? UserAgent { get; set; }

    [BsonElement("traceId")]
    public string TraceId { get; set; } = string.Empty;

    [BsonElement("metadata")]
    public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
}
