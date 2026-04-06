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
        public List<CaptainCoasterExternalVariantOption> ExternalVariants { get; set; } = new List<CaptainCoasterExternalVariantOption>();
    }

    public sealed class CaptainCoasterExternalVariantOption
    {
        [BsonElement("externalVariantId")]
        public string ExternalVariantId { get; set; } = string.Empty;

        [BsonElement("displayLabel")]
        public string DisplayLabel { get; set; } = string.Empty;

        [BsonElement("candidateLocalEntityId")]
        [BsonIgnoreIfNull]
        public string? CandidateLocalEntityId { get; set; }

        [BsonElement("sourceUrl")]
        [BsonIgnoreIfNull]
        public string? SourceUrl { get; set; }

        [BsonElement("isSuggested")]
        public bool IsSuggested { get; set; }

        [BsonElement("changes")]
        public List<CaptainCoasterFieldChange> Changes { get; set; } = new List<CaptainCoasterFieldChange>();
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
