using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Parks;

/// <summary>
/// Document embarqué des détails d'attraction.
/// </summary>
public sealed class AttractionDetailsDocument
{
    [BsonElement("manufacturerId")]
    [BsonIgnoreIfNull]
    public string? ManufacturerId { get; set; }

    [BsonElement("model")]
    [BsonIgnoreIfNull]
    public string? Model { get; set; }

    [BsonElement("externalSource")]
    [BsonIgnoreIfNull]
    public string? ExternalSource { get; set; }

    [BsonElement("externalId")]
    [BsonIgnoreIfNull]
    public string? ExternalId { get; set; }

    [BsonElement("sourceUrl")]
    [BsonIgnoreIfNull]
    public string? SourceUrl { get; set; }

    [BsonElement("status")]
    [BsonIgnoreIfNull]
    public string? Status { get; set; }

    [BsonElement("materialType")]
    [BsonIgnoreIfNull]
    public string? MaterialType { get; set; }

    [BsonElement("seatingType")]
    [BsonIgnoreIfNull]
    public string? SeatingType { get; set; }

    [BsonElement("launchType")]
    [BsonIgnoreIfNull]
    public string? LaunchType { get; set; }

    [BsonElement("restraintType")]
    [BsonIgnoreIfNull]
    public string? RestraintType { get; set; }

    [BsonElement("isLaunched")]
    [BsonIgnoreIfNull]
    public bool? IsLaunched { get; set; }

    [BsonElement("openingDate")]
    [BsonIgnoreIfNull]
    public DateTime? OpeningDate { get; set; }

    [BsonElement("closingDate")]
    [BsonIgnoreIfNull]
    public DateTime? ClosingDate { get; set; }

    [BsonElement("openingDateText")]
    [BsonIgnoreIfNull]
    public string? OpeningDateText { get; set; }

    [BsonElement("closingDateText")]
    [BsonIgnoreIfNull]
    public string? ClosingDateText { get; set; }

    [BsonElement("durationInSeconds")]
    [BsonIgnoreIfNull]
    public int? DurationInSeconds { get; set; }

    [BsonElement("capacityPerHour")]
    [BsonIgnoreIfNull]
    public int? CapacityPerHour { get; set; }

    [BsonElement("heightInFeet")]
    [BsonIgnoreIfNull]
    public double? HeightInFeet { get; set; }

    [BsonElement("heightInMeters")]
    [BsonIgnoreIfNull]
    public double? HeightInMeters { get; set; }

    [BsonElement("lengthInFeet")]
    [BsonIgnoreIfNull]
    public double? LengthInFeet { get; set; }

    [BsonElement("lengthInMeters")]
    [BsonIgnoreIfNull]
    public double? LengthInMeters { get; set; }

    [BsonElement("speedInMph")]
    [BsonIgnoreIfNull]
    public double? SpeedInMph { get; set; }

    [BsonElement("speedInKmH")]
    [BsonIgnoreIfNull]
    public double? SpeedInKmH { get; set; }

    [BsonElement("dropInFeet")]
    [BsonIgnoreIfNull]
    public double? DropInFeet { get; set; }

    [BsonElement("dropInMeters")]
    [BsonIgnoreIfNull]
    public double? DropInMeters { get; set; }

    [BsonElement("inversionCount")]
    [BsonIgnoreIfNull]
    public int? InversionCount { get; set; }

    [BsonElement("trainCount")]
    [BsonIgnoreIfNull]
    public int? TrainCount { get; set; }

    [BsonElement("carsPerTrain")]
    [BsonIgnoreIfNull]
    public int? CarsPerTrain { get; set; }

    [BsonElement("ridersPerVehicle")]
    [BsonIgnoreIfNull]
    public int? RidersPerVehicle { get; set; }

    [BsonElement("hasSingleRider")]
    [BsonIgnoreIfNull]
    public bool? HasSingleRider { get; set; }

    [BsonElement("hasFastPass")]
    [BsonIgnoreIfNull]
    public bool? HasFastPass { get; set; }

    [BsonElement("isAccessibleForReducedMobility")]
    [BsonIgnoreIfNull]
    public bool? IsAccessibleForReducedMobility { get; set; }

    [BsonElement("isIndoor")]
    [BsonIgnoreIfNull]
    public bool? IsIndoor { get; set; }

    [BsonElement("waterExposureLevel")]
    [BsonIgnoreIfNull]
    [BsonRepresentation(BsonType.String)]
    public AttractionWaterExposureLevel? WaterExposureLevel { get; set; }

    [BsonElement("accessConditions")]
    public List<AttractionAccessConditionDocument> AccessConditions { get; set; } = new();
}
