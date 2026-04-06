namespace AmusementPark.Core.Domain.Parks;

/// <summary>
/// Détails métier d'une attraction.
/// </summary>
public sealed class AttractionDetails
{
    public string? ManufacturerId { get; set; }

    public string? Model { get; set; }

    public string? ExternalSource { get; set; }

    public string? ExternalId { get; set; }

    public string? SourceUrl { get; set; }

    public string? Status { get; set; }

    public string? MaterialType { get; set; }

    public string? SeatingType { get; set; }

    public string? LaunchType { get; set; }

    public string? RestraintType { get; set; }

    public bool? IsLaunched { get; set; }

    public DateTime? OpeningDate { get; set; }

    public DateTime? ClosingDate { get; set; }

    public string? OpeningDateText { get; set; }

    public string? ClosingDateText { get; set; }

    public int? DurationInSeconds { get; set; }

    public int? CapacityPerHour { get; set; }

    public double? HeightInFeet { get; set; }

    public double? HeightInMeters { get; set; }

    public double? LengthInFeet { get; set; }

    public double? LengthInMeters { get; set; }

    public double? SpeedInMph { get; set; }

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

    public AttractionWaterExposureLevel? WaterExposureLevel { get; set; }

    public List<AttractionAccessCondition> AccessConditions { get; set; } = new();
}
