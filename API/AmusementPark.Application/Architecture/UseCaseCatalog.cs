namespace AmusementPark.Application.Architecture;

/// <summary>
/// Catalogue des cas d'usage applicatifs posés pendant la phase 4.
/// </summary>
public static class UseCaseCatalog
{
    /// <summary>
    /// Obtient la liste des cas d'usage par feature.
    /// </summary>
    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByFeature { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Countries"] = new[] { "GetCountries" },
            ["ParkFounders"] = new[] { "GetParkFounders", "GetParkFounderById", "CreateParkFounder", "UpdateParkFounder" },
            ["ParkOperators"] = new[] { "GetParkOperators", "GetParkOperatorById", "CreateParkOperator", "UpdateParkOperator" },
            ["AttractionManufacturers"] = new[] { "GetAttractionManufacturers", "GetAttractionManufacturerById", "CreateAttractionManufacturer", "UpdateAttractionManufacturer" },
            ["Parks"] = new[] { "CreatePark", "GetParkById", "GetParksPage", "SearchParksByName", "SearchParksByLocation", "UpdatePark", "UpdateParkVisibility" },
            ["ParkZones"] = new[] { "GetParkZonesByParkId", "GetParkZoneById", "CreateParkZone", "UpdateParkZone", "DeleteParkZone", "GetParkExplorer" },
            ["ParkItems"] = new[] { "GetParkItemsByParkId", "GetParkItemsPage", "GetParkItemById", "CreateParkItem", "UpdateParkItem", "DeleteParkItem" },
            ["Images"] = new[] { "UploadImage", "LinkImage", "SetCurrentImage", "DeleteImage", "GetImageById", "UpdateImageMetadata", "ListImageTags", "CreateImageTag", "UpdateImageTag" },
            ["Users"] = new[] { "RegisterLocalUser", "ProvisionExternalUser", "GetUserByEmail", "GetUserById", "UpdateUserProfile", "Login", "LoginExternal", "RefreshToken", "ConfirmEmail", "ResendConfirmationEmail", "ForgotPassword", "ResetPassword", "AssignRole", "RemoveRole", "LockUser", "UnlockUser", "GetUsersPage", "ChangePassword", "SynchronizeUserAvatar" },
            ["Search"] = new[] { "Search" },
            ["CaptainCoaster"] = new[] { "GetCaptainCoasterSettings", "UpdateCaptainCoasterSettings", "PreviewCaptainCoasterComparison", "StartCaptainCoasterImport", "ApplyCaptainCoasterChanges", "GetCaptainCoasterSession" },
        };
}
