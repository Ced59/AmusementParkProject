using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterComparisonResultDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

    [BsonElement("entityType")]
    public string EntityType { get; set; } = string.Empty;

    [BsonElement("changeType")]
    public string ChangeType { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("localEntityId")]
    [BsonIgnoreIfNull]
    public string? LocalEntityId { get; set; }

    [BsonElement("externalEntityId")]
    [BsonIgnoreIfNull]
    public string? ExternalEntityId { get; set; }

    [BsonElement("matchConfidence")]
    public string MatchConfidence { get; set; } = "Unknown";

    [BsonElement("changes")]
    public List<CaptainCoasterFieldChangeDocument> Changes { get; set; } = new List<CaptainCoasterFieldChangeDocument>();

    [BsonElement("isApplied")]
    public bool IsApplied { get; set; }

    [BsonElement("hasExternalDuplicates")]
    public bool HasExternalDuplicates { get; set; }

    [BsonElement("requiresManualResolution")]
    public bool RequiresManualResolution { get; set; }

    [BsonElement("resolutionStatus")]
    public string ResolutionStatus { get; set; } = "NotRequired";

    [BsonElement("appliedExternalVariantId")]
    [BsonIgnoreIfNull]
    public string? AppliedExternalVariantId { get; set; }

    [BsonElement("externalVariants")]
    public List<CaptainCoasterExternalVariantOptionDocument> ExternalVariants { get; set; } = new List<CaptainCoasterExternalVariantOptionDocument>();
}
