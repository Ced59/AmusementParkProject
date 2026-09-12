using AmusementPark.Core.Domain.Identifiers;

namespace AmusementPark.Application.Features.Sharing.Services;

public static class PublicCatalogShareSourceScope
{
    private const string ParkPrefix = "public-catalog:park:";

    public static string CreatePark(string parkId)
    {
        return string.Concat(
            ParkPrefix,
            IdentifierRules.NormalizeRequired(parkId, nameof(parkId)));
    }
}
