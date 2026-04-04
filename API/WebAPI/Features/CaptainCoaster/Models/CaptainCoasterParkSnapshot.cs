using Common.General;
using MongoDB.Bson.Serialization.Attributes;

namespace WebAPI.Features.CaptainCoaster.Models
{
    public sealed class CaptainCoasterParkSnapshot : GeolocatedEntity
    {
        [BsonElement("syncSessionId")]
        public string SyncSessionId { get; set; } = string.Empty;

        [BsonElement("externalSource")]
        public string ExternalSource { get; set; } = "CaptainCoaster";

        [BsonElement("captainCoasterId")]
        public string CaptainCoasterId { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("slug")]
        [BsonIgnoreIfNull]
        public string? Slug { get; set; }

        [BsonElement("sourceUrl")]
        [BsonIgnoreIfNull]
        public string? SourceUrl { get; set; }

        [BsonElement("countryCode")]
        [BsonIgnoreIfNull]
        public string? CountryCode { get; set; }

        [BsonElement("countryRaw")]
        [BsonIgnoreIfNull]
        public string? CountryRaw { get; set; }

        [BsonElement("coasterCount")]
        [BsonIgnoreIfNull]
        public int? CoasterCount { get; set; }

        [BsonElement("sampleCoasterNames")]
        public List<string> SampleCoasterNames { get; set; } = new List<string>();

        [BsonElement("scrapedAtUtc")]
        [BsonIgnoreIfNull]
        public DateTime? ScrapedAtUtc { get; set; }
    }
}
