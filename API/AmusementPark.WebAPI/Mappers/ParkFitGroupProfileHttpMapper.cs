using AmusementPark.Application.Features.ParkFit.Models;
using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.WebAPI.Contracts.ParkFit;

namespace AmusementPark.WebAPI.Mappers;

public static class ParkFitGroupProfileHttpMapper
{
    public static ParkFitGroupProfileInput ToApplication(
        this CreateParkFitGroupProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ParkFitGroupProfileInput(
            request.Alias,
            request.HeightCentimeters,
            request.AgeYears,
            request.CanBeAccompanied,
            request.CompanionAgeYears);
    }

    public static ParkFitGroupProfileInput ToApplication(
        this UpdateParkFitGroupProfileRequestDto request)
    {
        ArgumentNullException.ThrowIfNull(request);
        return new ParkFitGroupProfileInput(
            request.Alias,
            request.HeightCentimeters,
            request.AgeYears,
            request.CanBeAccompanied,
            request.CompanionAgeYears);
    }

    public static ParkFitGroupProfileDto ToHttp(this ParkFitGroupProfileResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ParkFitGroupProfileDto
        {
            ProfileId = result.ProfileId,
            Alias = result.Alias,
            HeightCentimeters = result.HeightCentimeters,
            AgeYears = result.AgeYears,
            CanBeAccompanied = result.CanBeAccompanied,
            CompanionAgeYears = result.CompanionAgeYears,
            CreatedAtUtc = result.CreatedAtUtc,
            UpdatedAtUtc = result.UpdatedAtUtc,
            Version = result.Version,
        };
    }

    public static ParkFitGroupProfileExportDto ToHttp(
        this ParkFitGroupProfileExportResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new ParkFitGroupProfileExportDto
        {
            ExportedAtUtc = result.ExportedAtUtc,
            Profiles = result.Profiles.Select(static profile =>
                new ParkFitGroupProfileExportItemDto
                {
                    Alias = profile.Alias,
                    HeightCentimeters = profile.HeightCentimeters,
                    AgeYears = profile.AgeYears,
                    CanBeAccompanied = profile.CanBeAccompanied,
                    CompanionAgeYears = profile.CompanionAgeYears,
                }).ToArray(),
        };
    }
}
