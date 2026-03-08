using Common.General;
using Common.General.Localization;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    public class ParkItem : GeolocatedEntity
    {
        [BsonElement("parkId")]
        public string ParkId { get; set; } = string.Empty;

        [BsonElement("zoneId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public string? ZoneId { get; set; }

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("category")]
        [BsonRepresentation(BsonType.String)]
        public ParkItemCategory Category { get; set; }

        [BsonElement("type")]
        [BsonRepresentation(BsonType.String)]
        public ParkItemType Type { get; set; }

        [BsonElement("subtype")]
        [BsonIgnoreIfNull]
        public string? Subtype { get; set; }

        [BsonElement("descriptions")]
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        [BsonElement("isVisible")]
        public bool IsVisible { get; set; } = true;
    }
}
