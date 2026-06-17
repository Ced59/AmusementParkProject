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
            ["ParkOperators"] = new[] { "GetParkOperators", "GetParkOperatorById", "CreateParkOperator", "UpdateParkOperator", "UpdateParkOperatorsBulkReviewStatus" },
            ["AttractionManufacturers"] = new[] { "GetAttractionManufacturers", "GetAttractionManufacturerById", "CreateAttractionManufacturer", "UpdateAttractionManufacturer", "UpdateAttractionManufacturersBulkReviewStatus" },
            ["Parks"] = new[] { "CreatePark", "GetParkById", "GetParksPage", "GetVisibleParkMapPoints", "SearchParksByName", "SearchParksByLocation", "CalculateParkDistances", "GetNearestParks", "UpdatePark", "UpdateParkVisibility", "UpdateParksBulkAdministration" },
            ["ParkZones"] = new[] { "GetParkZonesByParkId", "GetParkZoneById", "CreateParkZone", "UpdateParkZone", "DeleteParkZone", "GetParkExplorer" },
            ["ParkItems"] = new[] { "GetParkItemsByParkId", "GetParkItemsPage", "GetParkItemById", "CreateParkItem", "UpdateParkItem", "DeleteParkItem", "UpdateParkItemsBulkAdministration", "UpdateParkItemsBulkFields", "PreviewParkItemsBulkCreate", "ApplyParkItemsBulkCreate" },
            ["Images"] = new[] { "UploadImage", "LinkImage", "SetCurrentImage", "DeleteImage", "GetImageById", "GetCurrentImage", "GetImagesByOwner", "GetAllImages", "GetImagesPage", "UpdateImageMetadata", "UpdateImagesBulkMetadata", "ListImageTags", "CreateImageTag", "UpdateImageTag" },
            ["Videos"] = new[] { "CreateVideo", "UpdateVideo", "DeleteVideo", "GetVideoById", "GetVideosPage", "ResolveVideoMetadata", "ListVideoTags", "CreateVideoTag", "UpdateVideoTag" },
            ["Users"] = new[] { "RegisterLocalUser", "ProvisionExternalUser", "GetUserByEmail", "GetUserById", "UpdateUserProfile", "Login", "LoginExternal", "RefreshToken", "ConfirmEmail", "ResendConfirmationEmail", "ForgotPassword", "ResetPassword", "AssignRole", "RemoveRole", "LockUser", "UnlockUser", "GetUsersPage", "ChangePassword", "SynchronizeUserAvatar" },
            ["Search"] = new[] { "Search" },
            ["DataSources"] = new[] { "ListDataSources", "GetDataSourceStatus", "GetDataSourceSettings", "UpdateDataSourceSettings", "GetLatestDataSourceSession", "GetDataSourceSession", "GetDataSourceComparisonResults", "StartDataSourceImport", "ApplyDataSourceComparison" },
            ["AdminAudit"] = new[] { "GetAdminAuditLogs" },
        };
}
