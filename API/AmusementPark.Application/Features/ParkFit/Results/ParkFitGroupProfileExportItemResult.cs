namespace AmusementPark.Application.Features.ParkFit.Results;

public sealed record ParkFitGroupProfileExportItemResult(
    string Alias,
    int? HeightCentimeters,
    int? AgeYears,
    bool CanBeAccompanied,
    int? CompanionAgeYears);
