using Common.General;
using Common.General.Localization;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Images
{
    [BsonIgnoreExtraElements]
    public class Image : ModelBase
    {
        [BsonElement("category")]
        public ImageCategory Category { get; set; }

        [BsonElement("path")]
        [BsonIgnoreIfNull]
        public string? Path { get; set; }

        [BsonElement("description")]
        [BsonIgnoreIfNull]
        public string? Description { get; set; }

        [BsonElement("altTexts")]
        public List<LocalizedItem<string>> AltTexts { get; set; } = new();

        [BsonElement("captions")]
        public List<LocalizedItem<string>> Captions { get; set; } = new();

        [BsonElement("credits")]
        public List<LocalizedItem<string>> Credits { get; set; } = new();

        [BsonElement("tagIds")]
        public List<string> TagIds { get; set; } = new();

        [BsonElement("geoLocation")]
        [BsonIgnoreIfNull]
        public ImageGeoLocation? GeoLocation { get; set; }

        [BsonElement("exifMetadata")]
        [BsonIgnoreIfNull]
        public ImageExifMetadata? ExifMetadata { get; set; }

        [BsonElement("width")]
        public int Width { get; set; }

        [BsonElement("height")]
        public int Height { get; set; }

        [BsonElement("sizeInBytes")]
        public long SizeInBytes { get; set; }

        [BsonElement("ownerType")]
        public ImageOwnerType OwnerType { get; set; } = ImageOwnerType.None;

        [BsonElement("ownerId")]
        [BsonIgnoreIfNull]
        public string? OwnerId { get; set; }

        [BsonElement("isCurrent")]
        public bool IsCurrent { get; set; }

        [BsonElement("originalFileName")]
        [BsonIgnoreIfNull]
        public string? OriginalFileName { get; set; }

        [BsonElement("contentType")]
        [BsonIgnoreIfNull]
        public string? ContentType { get; set; }

        [BsonElement("isPublished")]
        public bool IsPublished { get; set; } = true;
    }
}
