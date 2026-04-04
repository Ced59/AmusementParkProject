using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterDataSourceSettings : ModelBase
    {
        [BsonElement("source")]
        public string Source { get; set; } = "CaptainCoaster";

        [BsonElement("sourceKey")]
        public string SourceKey { get; set; } = "captain-coaster";

        [BsonElement("displayName")]
        public string DisplayName { get; set; } = "Captain Coaster";

        [BsonElement("description")]
        public string Description { get; set; } = "Import de données JSON Captain Coaster avec staging, analyse et application sélective.";

        [BsonElement("inputMode")]
        public string InputMode { get; set; } = "JsonImport";

        [BsonElement("lastSuccessfulImportUtc")]
        [BsonIgnoreIfNull]
        public DateTime? LastSuccessfulImportUtc { get; set; }

        [BsonElement("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}
