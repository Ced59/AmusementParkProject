using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.CaptainCoaster;

public sealed class CaptainCoasterCoasterSnapshotDocument : MongoDocumentBase
{
    [BsonElement("sourceKey")]
    public string SourceKey { get; set; } = "captain-coaster";

    [BsonElement("syncSessionId")]
    public string SyncSessionId { get; set; } = string.Empty;

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

    [BsonElement("parkCaptainCoasterId")]
    [BsonIgnoreIfNull]
    public string? ParkCaptainCoasterId { get; set; }

    [BsonElement("parkName")]
    [BsonIgnoreIfNull]
    public string? ParkName { get; set; }

    [BsonElement("countryCode")]
    [BsonIgnoreIfNull]
    public string? CountryCode { get; set; }

    [BsonElement("countryRaw")]
    [BsonIgnoreIfNull]
    public string? CountryRaw { get; set; }

    [BsonElement("manufacturer")]
    [BsonIgnoreIfNull]
    public string? Manufacturer { get; set; }

    [BsonElement("model")]
    [BsonIgnoreIfNull]
    public string? Model { get; set; }

    [BsonElement("materialType")]
    [BsonIgnoreIfNull]
    public string? MaterialType { get; set; }

    [BsonElement("seatingType")]
    [BsonIgnoreIfNull]
    public string? SeatingType { get; set; }

    [BsonElement("launchType")]
    [BsonIgnoreIfNull]
    public string? LaunchType { get; set; }

    [BsonElement("restraint")]
    [BsonIgnoreIfNull]
    public string? Restraint { get; set; }

    [BsonElement("isLaunched")]
    public bool IsLaunched { get; set; }

    [BsonElement("speedInKmH")]
    [BsonIgnoreIfNull]
    public double? SpeedInKmH { get; set; }

    [BsonElement("heightInMeters")]
    [BsonIgnoreIfNull]
    public double? HeightInMeters { get; set; }

    [BsonElement("lengthInMeters")]
    [BsonIgnoreIfNull]
    public double? LengthInMeters { get; set; }

    [BsonElement("dropInMeters")]
    [BsonIgnoreIfNull]
    public double? DropInMeters { get; set; }

    [BsonElement("inversionCount")]
    [BsonIgnoreIfNull]
    public int? InversionCount { get; set; }

    [BsonElement("status")]
    [BsonIgnoreIfNull]
    public string? Status { get; set; }

    [BsonElement("openingDate")]
    [BsonIgnoreIfNull]
    public DateTime? OpeningDate { get; set; }

    [BsonElement("closingDate")]
    [BsonIgnoreIfNull]
    public DateTime? ClosingDate { get; set; }

    [BsonElement("scrapedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? ScrapedAtUtc { get; set; }
}
