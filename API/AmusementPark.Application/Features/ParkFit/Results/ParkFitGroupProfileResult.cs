namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitGroupProfileResult(
    string ProfileId,
    string Alias,
    int? HeightCentimeters,
    int? AgeYears,
    bool CanBeAccompanied,
    int? CompanionAgeYears,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    long Version);
