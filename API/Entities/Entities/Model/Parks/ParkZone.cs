using System.Collections.Generic;
using Common.General;
using Common.General.Localization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    [BsonIgnoreExtraElements]
    public class ParkZone : ModelBase
    {
        [BsonElement("parkId")]
        [BsonRepresentation(BsonType.String)]
        public string ParkId { get; set; } = string.Empty;

        // Legacy single name field kept for backward compatibility with existing Mongo documents.
        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("names")]
        public List<LocalizedItem<string>> Names { get; set; } = new();

        [BsonElement("slug")]
        [BsonIgnoreIfNull]
        public string? Slug { get; set; }

        [BsonElement("descriptions")]
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        [BsonElement("latitude")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? Latitude { get; set; }

        [BsonElement("longitude")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? Longitude { get; set; }

        [BsonElement("isVisible")]
        public bool IsVisible { get; set; } = true;

        [BsonElement("sortOrder")]
        public int SortOrder { get; set; }
    }
}
