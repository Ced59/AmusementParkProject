using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterDataSourceSettings : ModelBase
    {
        [BsonElement("source")]
        public string Source { get; set; } = "CaptainCoaster";

        [BsonElement("apiKey")]
        public string ApiKey { get; set; } = string.Empty;

        [BsonElement("baseUrl")]
        public string BaseUrl { get; set; } = "https://captaincoaster.com/api";

        [BsonElement("lastSuccessfulSyncUtc")]
        [BsonIgnoreIfNull]
        public DateTime? LastSuccessfulSyncUtc { get; set; }

        [BsonElement("isEnabled")]
        public bool IsEnabled { get; set; } = true;
    }
}
