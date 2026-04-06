using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Images
{
    public sealed class ImageTag : ModelBase
    {
        [BsonElement("slug")]
        public string Slug { get; set; } = string.Empty;

        [BsonElement("labels")]
        public List<LocalizedItem<string>> Labels { get; set; } = new();

        [BsonElement("descriptions")]
        public List<LocalizedItem<string>> Descriptions { get; set; } = new();

        [BsonElement("isActive")]
        public bool IsActive { get; set; } = true;
    }
}
