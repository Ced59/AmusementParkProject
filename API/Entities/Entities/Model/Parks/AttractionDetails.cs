using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace Entities.Model.Parks
{
    [BsonIgnoreExtraElements]
    public class AttractionDetails
    {
        [BsonElement("manufacturerId")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.String)]
        public string? ManufacturerId { get; set; }

        [BsonElement("model")]
        [BsonIgnoreIfNull]
        public string? Model { get; set; }

        [BsonElement("openingDate")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? OpeningDate { get; set; }

        [BsonElement("closingDate")]
        [BsonIgnoreIfNull]
        [BsonDateTimeOptions(Kind = DateTimeKind.Utc)]
        public DateTime? ClosingDate { get; set; }

        [BsonElement("durationInSeconds")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? DurationInSeconds { get; set; }

        [BsonElement("capacityPerHour")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? CapacityPerHour { get; set; }

        [BsonElement("heightInMeters")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? HeightInMeters { get; set; }

        [BsonElement("lengthInMeters")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? LengthInMeters { get; set; }

        [BsonElement("speedInKmH")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? SpeedInKmH { get; set; }

        [BsonElement("dropInMeters")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? DropInMeters { get; set; }

        [BsonElement("inversionCount")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? InversionCount { get; set; }

        [BsonElement("trainCount")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? TrainCount { get; set; }

        [BsonElement("carsPerTrain")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
        public int? CarsPerTrain { get; set; }

        [BsonElement("ridersPerVehicle")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Int32)]
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
        [BsonIgnoreIfNull]
        public List<AttractionAccessCondition>? AccessConditions { get; set; }
    }
}
