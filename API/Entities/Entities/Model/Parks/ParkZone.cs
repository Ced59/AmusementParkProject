using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    public class ParkZone : ModelBase
    {
        [BsonElement("parkId")]
        public string ParkId { get; set; } = string.Empty;

        [BsonElement("name")]
        public string Name { get; set; } = string.Empty;

        [BsonElement("slug")]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("descriptions")]
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        [BsonElement("isVisible")]
        public bool IsVisible { get; set; } = true;

        [BsonElement("sortOrder")]
        public int SortOrder { get; set; }
    }
}
