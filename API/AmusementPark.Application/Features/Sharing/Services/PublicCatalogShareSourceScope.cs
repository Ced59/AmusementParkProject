using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PublicCatalogShareSourceScope
{
    private const string PassportActivityParkPrefix = "public-catalog:passport-activity:park:";
    private const string PassportGeographyParkPrefix = "public-catalog:passport-geography:park:";
    private const string PassportMissedItemsParkPrefix = "public-catalog:passport-missed-items:park:";
    private const string PassportRatingsParkPrefix = "public-catalog:passport-ratings:park:";

    public static string CreatePassportActivityPark(string parkId)
    {
        return string.Concat(
            PassportActivityParkPrefix,
            IdentifierRules.NormalizeRequired(parkId, nameof(parkId)));
    }

    public static string CreatePassportGeographyPark(string parkId)
    {
        return string.Concat(
            PassportGeographyParkPrefix,
            IdentifierRules.NormalizeRequired(parkId, nameof(parkId)));
    }

    public static string CreatePassportMissedItemsPark(string parkId)
    {
        return string.Concat(
            PassportMissedItemsParkPrefix,
            IdentifierRules.NormalizeRequired(parkId, nameof(parkId)));
    }

    public static string CreatePassportRatingsPark(string parkId)
    {
        return string.Concat(
            PassportRatingsParkPrefix,
            IdentifierRules.NormalizeRequired(parkId, nameof(parkId)));
    }
}
