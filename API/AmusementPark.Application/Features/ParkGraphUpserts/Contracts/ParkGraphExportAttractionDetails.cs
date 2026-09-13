using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Geo;
using AmusementPark.Core.Localization;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Contracts;

public sealed class ParkGraphExportAttractionDetails
{
    public string? ManufacturerId { get; init; }

    public string? ManufacturerKey { get; init; }

    public string? Model { get; init; }

    public string? ExternalSource { get; init; }

    public string? ExternalId { get; init; }

    public string? SourceUrl { get; init; }

    public string? Status { get; init; }

    public string? MaterialType { get; init; }

    public string? SeatingType { get; init; }

    public string? LaunchType { get; init; }

    public string? RestraintType { get; init; }

    public bool? IsLaunched { get; init; }

    public DateTime? OpeningDate { get; init; }

    public DateTime? ClosingDate { get; init; }

    public string? OpeningDateText { get; init; }

    public string? ClosingDateText { get; init; }

    public int? DurationInSeconds { get; init; }

    public int? CapacityPerHour { get; init; }

    public double? HeightInFeet { get; init; }

    public double? HeightInMeters { get; init; }

    public double? LengthInFeet { get; init; }

    public double? LengthInMeters { get; init; }

    public double? SpeedInMph { get; init; }

    public double? SpeedInKmH { get; init; }

    public double? DropInFeet { get; init; }

    public double? DropInMeters { get; init; }

    public int? InversionCount { get; init; }

    public int? TrainCount { get; init; }

    public int? CarsPerTrain { get; init; }

    public int? RidersPerVehicle { get; init; }

    public bool? HasSingleRider { get; init; }

    public bool? HasFastPass { get; init; }

    public bool? IsAccessibleForReducedMobility { get; init; }

    public bool? IsIndoor { get; init; }

    public AttractionWaterExposureLevel? WaterExposureLevel { get; init; }

    public List<AttractionAccessCondition> AccessConditions { get; init; } = new List<AttractionAccessCondition>();
}
