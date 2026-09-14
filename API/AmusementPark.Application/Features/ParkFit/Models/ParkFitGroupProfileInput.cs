namespace AmusementPark.Application.Features.ParkFit.Models;

public sealed record ParkFitGroupProfileInput(
    string Alias,
    int? HeightCentimeters,
    int? AgeYears,
    bool CanBeAccompanied,
    int? CompanionAgeYears);
