using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;

/// <summary>
/// Document Mongo d'une image.
/// </summary>
[BsonIgnoreExtraElements]
public sealed class ImageDocument : MongoDocumentBase
{
    [BsonElement("category")]
    [BsonRepresentation(BsonType.String)]
    public ImageCategory Category { get; set; }

    [BsonElement("path")]
    [BsonIgnoreIfNull]
    public string? Path { get; set; }

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("altTexts")]
    public List<LocalizedTextDocument> AltTexts { get; set; } = new();

    [BsonElement("captions")]
    public List<LocalizedTextDocument> Captions { get; set; } = new();

    [BsonElement("credits")]
    public List<LocalizedTextDocument> Credits { get; set; } = new();

    [BsonElement("tagIds")]
    public List<string> TagIds { get; set; } = new();

    [BsonElement("geoLocation")]
    [BsonIgnoreIfNull]
    public GeoPointDocument? GeoLocation { get; set; }

    [BsonElement("exifMetadata")]
    [BsonIgnoreIfNull]
    public ImageExifMetadataDocument? ExifMetadata { get; set; }

    [BsonElement("width")]
    public int Width { get; set; }

    [BsonElement("height")]
    public int Height { get; set; }

    [BsonElement("sizeInBytes")]
    public long SizeInBytes { get; set; }

    [BsonElement("ownerType")]
    [BsonRepresentation(BsonType.String)]
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

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;
}

/// <summary>
/// Document Mongo d'un tag d'image.
/// </summary>
public sealed class ImageTagDocument : MongoDocumentBase
{
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}

/// <summary>
/// Document embarqué des métadonnées EXIF.
/// </summary>
public sealed class ImageExifMetadataDocument
{
    [BsonElement("cameraMaker")]
    [BsonIgnoreIfNull]
    public string? CameraMaker { get; set; }

    [BsonElement("cameraModel")]
    [BsonIgnoreIfNull]
    public string? CameraModel { get; set; }

    [BsonElement("takenOnUtc")]
    [BsonIgnoreIfNull]
    public DateTime? TakenOnUtc { get; set; }

    [BsonElement("orientation")]
    [BsonIgnoreIfNull]
    public string? Orientation { get; set; }

    [BsonElement("focalLength")]
    [BsonIgnoreIfNull]
    public double? FocalLength { get; set; }

    [BsonElement("aperture")]
    [BsonIgnoreIfNull]
    public double? Aperture { get; set; }

    [BsonElement("exposureTime")]
    [BsonIgnoreIfNull]
    public double? ExposureTime { get; set; }

    [BsonElement("iso")]
    [BsonIgnoreIfNull]
    public int? Iso { get; set; }

    [BsonElement("rawGpsLatitude")]
    [BsonIgnoreIfNull]
    public string? RawGpsLatitude { get; set; }

    [BsonElement("rawGpsLongitude")]
    [BsonIgnoreIfNull]
    public string? RawGpsLongitude { get; set; }
}
