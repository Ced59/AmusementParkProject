using System;
using System.Collections.Generic;

namespace Dtos.ParkItems
{
    public class AttractionDetailsDto
    {
        public string? ManufacturerId { get; set; }
        public string? Model { get; set; }
        public DateTime? OpeningDate { get; set; }
        public DateTime? ClosingDate { get; set; }
        public int? DurationInSeconds { get; set; }
        public int? CapacityPerHour { get; set; }
        public double? HeightInMeters { get; set; }
        public double? LengthInMeters { get; set; }
        public double? SpeedInKmH { get; set; }
        public double? DropInMeters { get; set; }
        public int? InversionCount { get; set; }
        public int? TrainCount { get; set; }
        public int? CarsPerTrain { get; set; }
        public int? RidersPerVehicle { get; set; }
        public bool? HasSingleRider { get; set; }
        public bool? HasFastPass { get; set; }
        public bool? IsAccessibleForReducedMobility { get; set; }
        public bool? IsIndoor { get; set; }
        public AttractionWaterExposureLevelDto? WaterExposureLevel { get; set; }
        public List<AttractionAccessConditionDto>? AccessConditions { get; set; }
    }
}
