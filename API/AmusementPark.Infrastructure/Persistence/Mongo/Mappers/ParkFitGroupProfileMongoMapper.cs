using AmusementPark.Core.Domain.ParkFit;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.ParkFit;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Mappers;

internal static class ParkFitGroupProfileMongoMapper
{
    public static ParkFitGroupProfileDocument ToDocument(this ParkFitGroupProfile profile)
    {
        ArgumentNullException.ThrowIfNull(profile);
        return new ParkFitGroupProfileDocument
        {
            Id = profile.Id.Value,
            OwnerUserId = profile.OwnerUserId,
            Alias = profile.Alias,
            NormalizedAlias = profile.NormalizedAlias,
            HeightCentimeters = profile.HeightCentimeters,
            AgeYears = profile.AgeYears,
            CanBeAccompanied = profile.CanBeAccompanied,
            CompanionAgeYears = profile.CompanionAgeYears,
            Version = profile.Version,
            CreatedAt = profile.CreatedAtUtc,
            UpdatedAt = profile.UpdatedAtUtc,
        };
    }

    public static ParkFitGroupProfile ToDomain(this ParkFitGroupProfileDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        return ParkFitGroupProfile.Restore(
            ParkFitGroupProfileId.Parse(document.Id),
            document.OwnerUserId,
            document.Alias,
            document.HeightCentimeters,
            document.AgeYears,
            document.CanBeAccompanied,
            document.CompanionAgeYears,
            document.CreatedAt,
            document.UpdatedAt,
            document.Version);
    }
}
