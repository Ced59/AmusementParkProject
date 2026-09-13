using AmusementPark.Core.Domain.Images;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Images;

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
