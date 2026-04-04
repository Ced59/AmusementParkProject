using System;
using System.Collections.Generic;
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

        [BsonElement("openingDateText")]
        [BsonIgnoreIfNull]
        public string? OpeningDateText { get; set; }

        [BsonElement("closingDateText")]
        [BsonIgnoreIfNull]
        public string? ClosingDateText { get; set; }

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

        [BsonElement("heightInFeet")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? HeightInFeet { get; set; }

        [BsonElement("heightInMeters")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? HeightInMeters { get; set; }

        [BsonElement("lengthInFeet")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? LengthInFeet { get; set; }

        [BsonElement("lengthInMeters")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? LengthInMeters { get; set; }

        [BsonElement("speedInMph")]
        [BsonIgnoreIfNull]
        [BsonRepresentation(BsonType.Double)]
        public double? SpeedInMph { get; set; }

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
