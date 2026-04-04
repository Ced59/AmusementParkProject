using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterComparisonResult : ModelBase
    {
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
        public List<CaptainCoasterFieldChange> Changes { get; set; } = new List<CaptainCoasterFieldChange>();

        [BsonElement("isApplied")]
        public bool IsApplied { get; set; }
    }

    public sealed class CaptainCoasterFieldChange
    {
        [BsonElement("field")]
        public string Field { get; set; } = string.Empty;

        [BsonElement("localValue")]
        [BsonIgnoreIfNull]
        public string? LocalValue { get; set; }

        [BsonElement("externalValue")]
        [BsonIgnoreIfNull]
        public string? ExternalValue { get; set; }

        [BsonElement("isDifferent")]
        public bool IsDifferent { get; set; }
    }
}
