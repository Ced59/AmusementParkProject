using AmusementPark.Application.Features.ParkFit.Results;
using AmusementPark.Core.Domain.ParkFit;

namespace AmusementPark.Application.Features.ParkFit.Services;

internal static class ParkFitGroupProfileResultMapper
{
    public static ParkFitGroupProfileResult ToResult(this ParkFitGroupProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new ParkFitGroupProfileResult(
            profile.Id.Value,
            profile.Alias,
            profile.HeightCentimeters,
            profile.AgeYears,
            profile.CanBeAccompanied,
            profile.CompanionAgeYears,
            profile.CreatedAtUtc,
            profile.UpdatedAtUtc,
            profile.Version);
    }
}
