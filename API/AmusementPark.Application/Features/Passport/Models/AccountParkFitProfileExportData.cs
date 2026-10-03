namespace AmusementPark.Application.Features.Passport.Models;

public sealed record AccountParkFitProfileExportData(
    string Alias,
    int? HeightCentimeters,
    int? AgeYears,
    bool CanBeAccompanied,
    int? CompanionAgeYears,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc);
