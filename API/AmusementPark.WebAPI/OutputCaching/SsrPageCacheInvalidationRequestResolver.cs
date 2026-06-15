using System.Reflection;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Mvc.Filters;

namespace AmusementPark.WebAPI.OutputCaching;

public sealed class SsrPageCacheInvalidationRequestResolver : ISsrPageCacheInvalidationRequestResolver
{
    private const int MaxTargetedEntityCount = 100;

    private static readonly IReadOnlyCollection<string> PublicLanguages = new[]
    {
        "fr",
        "en",
        "de",
        "nl",
        "pl",
        "pt",
        "it",
        "es",
    };

    private readonly IParkRepository parkRepository;
    private readonly IParkItemRepository parkItemRepository;
    private readonly IParkZoneRepository parkZoneRepository;
    private readonly IImageRepository imageRepository;

    public SsrPageCacheInvalidationRequestResolver(
        IParkRepository parkRepository,
        IParkItemRepository parkItemRepository,
        IParkZoneRepository parkZoneRepository,
        IImageRepository imageRepository)
    {
        this.parkRepository = parkRepository;
        this.parkItemRepository = parkItemRepository;
        this.parkZoneRepository = parkZoneRepository;
        this.imageRepository = imageRepository;
    }

    public async Task<SsrPageCacheInvalidationRequest> ResolveAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        IReadOnlyCollection<PublicCacheScope> scopes,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(scopes);

        bool includeSeoDocuments = scopes.Count > 0;
        bool hasPageImpact = scopes.Contains(PublicCacheScope.Data) || scopes.Contains(PublicCacheScope.ReferenceData);

        if (!hasPageImpact)
        {
            return BuildRequest(Array.Empty<string>(), Array.Empty<string>(), includeSeoDocuments);
        }

        string controllerName = ResolveControllerName(context);
        SsrPageCacheInvalidationRequest request = controllerName switch
        {
            "Parks" => await this.ResolveParksAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ParkItems" => await this.ResolveParkItemsAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ParkZones" => await this.ResolveParkZonesAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ParkOperators" => await this.ResolveParkOperatorAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ParkFounders" => await this.ResolveParkFounderAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "AttractionManufacturers" => await this.ResolveAttractionManufacturerAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "LocalizedContent" => await this.ResolveLocalizedContentAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "Images" => await this.ResolveImagesAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ParkGraphUpserts" => await this.ResolveParkGraphUpsertAsync(context, executedContext, includeSeoDocuments),
            _ => SsrPageCacheInvalidationRequest.AllCaches(),
        };

        return request;
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveParksAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> parkIds = ResolveStringTargets(context, executedContext, "id", "Id", "Ids");

        if (parkIds.Count == 0)
        {
            IReadOnlyCollection<string> administrationIds = await this.ResolveParkAdministrationIdsAsync(context, cancellationToken);
            parkIds = administrationIds;
        }

        if (parkIds.Count == 0 || parkIds.Count > MaxTargetedEntityCount)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        return BuildParkImpactRequest(parkIds, includeSeoDocuments, includeDiscoveryPages: true);
    }

    private async Task<IReadOnlyCollection<string>> ResolveParkAdministrationIdsAsync(ActionExecutingContext context, CancellationToken cancellationToken)
    {
        object? request = FindActionArgument(context, "request");
        if (request is null)
        {
            return Array.Empty<string>();
        }

        IReadOnlyCollection<string> explicitIds = GetStringCollectionProperty(request, "Ids");
        if (explicitIds.Count > 0)
        {
            return explicitIds;
        }

        bool? isVisible = GetNullableBooleanProperty(request, "FilterIsVisible");
        string? adminReviewStatusText = GetStringProperty(request, "FilterAdminReviewStatus");
        string? typeText = GetStringProperty(request, "FilterType");
        string? countryCode = GetStringProperty(request, "FilterCountryCode");
        bool? hasValidCoordinates = GetNullableBooleanProperty(request, "FilterHasValidCoordinates");
        AdminReviewStatus? adminReviewStatus = ParseEnum<AdminReviewStatus>(adminReviewStatusText);
        ParkType? type = ParseEnum<ParkType>(typeText);

        return await this.parkRepository.GetAdministrationIdsAsync(
            includeHidden: true,
            isVisible,
            adminReviewStatus,
            type,
            countryCode,
            hasValidCoordinates,
            cancellationToken);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveParkItemsAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        HashSet<string> parkIds = new HashSet<string>(StringComparer.Ordinal);

        AddNonEmpty(parkIds, GetStringProperty(ResolveResultValue(executedContext), "ParkId"));
        AddNonEmpty(parkIds, GetStringProperty(FindActionArgument(context, "dto"), "ParkId"));
        AddNonEmpty(parkIds, GetStringProperty(FindActionArgument(context, "request"), "ParkId"));

        HashSet<string> itemIds = new HashSet<string>(ResolveStringTargets(context, executedContext, "id", "Id", "Ids"), StringComparer.Ordinal);
        IReadOnlyCollection<string> requestIds = GetStringCollectionProperty(FindActionArgument(context, "request"), "Ids");
        AddRange(itemIds, requestIds);

        if (itemIds.Count > MaxTargetedEntityCount)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (itemIds.Count > 0)
        {
            IReadOnlyCollection<ParkItem> items = await this.parkItemRepository.GetByIdsAsync(itemIds, cancellationToken);
            foreach (ParkItem item in items)
            {
                AddNonEmpty(parkIds, item.ParkId);
            }
        }

        if (parkIds.Count == 0)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        return BuildParkImpactRequest(parkIds, includeSeoDocuments, includeDiscoveryPages: false);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveParkZonesAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        HashSet<string> parkIds = new HashSet<string>(StringComparer.Ordinal);

        AddNonEmpty(parkIds, GetStringProperty(ResolveResultValue(executedContext), "ParkId"));
        AddNonEmpty(parkIds, GetStringProperty(FindActionArgument(context, "dto"), "ParkId"));

        IReadOnlyCollection<string> zoneIds = ResolveStringTargets(context, executedContext, "id", "Id", "Ids");

        if (zoneIds.Count > MaxTargetedEntityCount)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        foreach (string zoneId in zoneIds)
        {
            ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(zoneId, cancellationToken);
            if (zone is not null)
            {
                AddNonEmpty(parkIds, zone.ParkId);
            }
        }

        if (parkIds.Count == 0)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        return BuildParkImpactRequest(parkIds, includeSeoDocuments, includeDiscoveryPages: false);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveParkOperatorAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> operatorIds = ResolveStringTargets(context, executedContext, "id", "Id", "Ids");
        return await this.ResolveReferenceImpactAsync(
            operatorIds,
            "operator",
            id => this.parkRepository.GetParkIdsByOperatorIdAsync(id, cancellationToken),
            includeSeoDocuments);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveParkFounderAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> founderIds = ResolveStringTargets(context, executedContext, "id", "Id", "Ids");
        return await this.ResolveReferenceImpactAsync(
            founderIds,
            "founder",
            id => this.parkRepository.GetParkIdsByFounderIdAsync(id, cancellationToken),
            includeSeoDocuments);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveAttractionManufacturerAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<string> manufacturerIds = ResolveStringTargets(context, executedContext, "id", "Id", "Ids");
        return await this.ResolveReferenceImpactAsync(
            manufacturerIds,
            "manufacturer",
            id => this.parkItemRepository.GetParkIdsByManufacturerIdAsync(id, cancellationToken),
            includeSeoDocuments);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveReferenceImpactAsync(
        IReadOnlyCollection<string> referenceIds,
        string referenceKind,
        Func<string, Task<IReadOnlyCollection<string>>> parkIdsResolver,
        bool includeSeoDocuments)
    {
        if (referenceIds.Count == 0 || referenceIds.Count > MaxTargetedEntityCount)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> parkIds = new HashSet<string>(StringComparer.Ordinal);

        foreach (string referenceId in referenceIds)
        {
            AddReferencePrefixes(prefixes, referenceKind, referenceId);
            IReadOnlyCollection<string> resolvedParkIds = await parkIdsResolver(referenceId);
            AddRange(parkIds, resolvedParkIds);
        }

        AddParkPrefixes(prefixes, parkIds);
        AddDiscoveryPaths(paths);

        return BuildRequest(paths, prefixes, includeSeoDocuments);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveLocalizedContentAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        string? entityType = GetRouteValue(context, "entityType") ?? GetStringProperty(ResolveResultValue(executedContext), "EntityType");
        string? entityId = GetRouteValue(context, "entityId") ?? GetStringProperty(ResolveResultValue(executedContext), "EntityId");

        if (string.IsNullOrWhiteSpace(entityType) || string.IsNullOrWhiteSpace(entityId))
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        return await this.ResolveEntityImpactAsync(entityType, entityId, includeSeoDocuments, cancellationToken);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveImagesAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        object? resultValue = ResolveResultValue(executedContext);
        string? ownerType = GetPropertyText(resultValue, "OwnerType") ?? GetPropertyText(FindActionArgument(context, "request"), "OwnerType");
        string? ownerId = GetStringProperty(resultValue, "OwnerId") ?? GetStringProperty(FindActionArgument(context, "request"), "OwnerId");

        if (string.IsNullOrWhiteSpace(ownerType) || string.IsNullOrWhiteSpace(ownerId))
        {
            string? imageId = GetRouteValue(context, "imageId");
            if (!string.IsNullOrWhiteSpace(imageId))
            {
                Image? image = await this.imageRepository.GetByIdAsync(imageId, cancellationToken);
                ownerType = image?.OwnerType.ToString();
                ownerId = image?.OwnerId;
            }
        }

        if (string.IsNullOrWhiteSpace(ownerType) || string.IsNullOrWhiteSpace(ownerId))
        {
            return BuildRequest(Array.Empty<string>(), Array.Empty<string>(), includeSeoDocuments);
        }

        return await this.ResolveEntityImpactAsync(ownerType, ownerId, includeSeoDocuments, cancellationToken);
    }

    private Task<SsrPageCacheInvalidationRequest> ResolveParkGraphUpsertAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments)
    {
        HashSet<string> parkIds = new HashSet<string>(StringComparer.Ordinal);
        AddNonEmpty(parkIds, GetStringProperty(FindActionArgument(context, "request"), "TargetParkId"));
        AddNonEmpty(parkIds, GetStringProperty(ResolveResultValue(executedContext), "TargetParkId"));

        SsrPageCacheInvalidationRequest request = parkIds.Count == 0
            ? SsrPageCacheInvalidationRequest.AllCaches()
            : BuildParkImpactRequest(parkIds, includeSeoDocuments, includeDiscoveryPages: false);

        return Task.FromResult(request);
    }

    private async Task<SsrPageCacheInvalidationRequest> ResolveEntityImpactAsync(
        string entityType,
        string entityId,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        string normalizedEntityType = NormalizeEntityType(entityType);
        string normalizedEntityId = entityId.Trim();

        if (string.Equals(normalizedEntityType, "park", StringComparison.Ordinal))
        {
            return BuildParkImpactRequest(new[] { normalizedEntityId }, includeSeoDocuments, includeDiscoveryPages: true);
        }

        if (string.Equals(normalizedEntityType, "parkitem", StringComparison.Ordinal) || string.Equals(normalizedEntityType, "attraction", StringComparison.Ordinal))
        {
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(normalizedEntityId, true, cancellationToken);
            return item is null
                ? SsrPageCacheInvalidationRequest.AllCaches()
                : BuildParkImpactRequest(new[] { item.ParkId }, includeSeoDocuments, includeDiscoveryPages: false);
        }

        if (string.Equals(normalizedEntityType, "parkzone", StringComparison.Ordinal) || string.Equals(normalizedEntityType, "zone", StringComparison.Ordinal))
        {
            ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(normalizedEntityId, cancellationToken);
            return zone is null
                ? SsrPageCacheInvalidationRequest.AllCaches()
                : BuildParkImpactRequest(new[] { zone.ParkId }, includeSeoDocuments, includeDiscoveryPages: false);
        }

        if (string.Equals(normalizedEntityType, "parkoperator", StringComparison.Ordinal) || string.Equals(normalizedEntityType, "operator", StringComparison.Ordinal))
        {
            return await this.ResolveReferenceImpactAsync(
                new[] { normalizedEntityId },
                "operator",
                id => this.parkRepository.GetParkIdsByOperatorIdAsync(id, cancellationToken),
                includeSeoDocuments);
        }

        if (string.Equals(normalizedEntityType, "parkfounder", StringComparison.Ordinal) || string.Equals(normalizedEntityType, "founder", StringComparison.Ordinal))
        {
            return await this.ResolveReferenceImpactAsync(
                new[] { normalizedEntityId },
                "founder",
                id => this.parkRepository.GetParkIdsByFounderIdAsync(id, cancellationToken),
                includeSeoDocuments);
        }

        if (string.Equals(normalizedEntityType, "attractionmanufacturer", StringComparison.Ordinal) || string.Equals(normalizedEntityType, "manufacturer", StringComparison.Ordinal))
        {
            return await this.ResolveReferenceImpactAsync(
                new[] { normalizedEntityId },
                "manufacturer",
                id => this.parkItemRepository.GetParkIdsByManufacturerIdAsync(id, cancellationToken),
                includeSeoDocuments);
        }

        return SsrPageCacheInvalidationRequest.AllCaches();
    }

    private static SsrPageCacheInvalidationRequest BuildParkImpactRequest(
        IReadOnlyCollection<string> parkIds,
        bool includeSeoDocuments,
        bool includeDiscoveryPages)
    {
        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);

        AddParkPrefixes(prefixes, parkIds);

        if (includeDiscoveryPages)
        {
            AddDiscoveryPaths(paths);
        }

        return BuildRequest(paths, prefixes, includeSeoDocuments);
    }

    private static SsrPageCacheInvalidationRequest BuildRequest(
        IEnumerable<string> paths,
        IEnumerable<string> prefixes,
        bool includeSeoDocuments)
    {
        List<string> normalizedPaths = NormalizePaths(paths).ToList();
        List<string> normalizedPrefixes = NormalizePaths(prefixes).ToList();

        return new SsrPageCacheInvalidationRequest
        {
            All = false,
            Paths = normalizedPaths,
            Prefixes = normalizedPrefixes,
            IncludeSeoDocuments = includeSeoDocuments,
        };
    }

    private static void AddDiscoveryPaths(ISet<string> paths)
    {
        paths.Add("/");

        foreach (string language in PublicLanguages)
        {
            paths.Add($"/{language}");
            paths.Add($"/{language}/home");
            paths.Add($"/{language}/parks");
        }
    }

    private static void AddParkPrefixes(ISet<string> prefixes, IEnumerable<string> parkIds)
    {
        foreach (string parkId in NormalizeTargets(parkIds))
        {
            foreach (string language in PublicLanguages)
            {
                prefixes.Add($"/{language}/park/{parkId}/");
            }
        }
    }

    private static void AddReferencePrefixes(ISet<string> prefixes, string referenceKind, string referenceId)
    {
        foreach (string language in PublicLanguages)
        {
            prefixes.Add($"/{language}/park-{referenceKind}/{referenceId}/");
        }
    }

    private static IReadOnlyCollection<string> ResolveStringTargets(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        string routeKey,
        string resultPropertyName,
        string collectionPropertyName)
    {
        HashSet<string> targets = new HashSet<string>(StringComparer.Ordinal);

        AddNonEmpty(targets, GetRouteValue(context, routeKey));
        AddNonEmpty(targets, GetStringProperty(ResolveResultValue(executedContext), resultPropertyName));
        AddRange(targets, GetStringCollectionProperty(FindActionArgument(context, "request"), collectionPropertyName));

        return targets.ToList();
    }

    private static object? ResolveResultValue(ActionExecutedContext? executedContext)
    {
        if (executedContext?.Result is ObjectResult objectResult)
        {
            return objectResult.Value;
        }

        return null;
    }

    private static object? FindActionArgument(ActionExecutingContext context, string argumentName)
    {
        return context.ActionArguments.TryGetValue(argumentName, out object? value) ? value : null;
    }

    private static string ResolveControllerName(ActionExecutingContext context)
    {
        if (context.ActionDescriptor is ControllerActionDescriptor controllerActionDescriptor)
        {
            return controllerActionDescriptor.ControllerName;
        }

        return string.Empty;
    }

    private static string? GetRouteValue(ActionExecutingContext context, string key)
    {
        object? value = context.RouteData.Values.TryGetValue(key, out object? routeValue)
            ? routeValue
            : null;

        return value?.ToString();
    }

    private static string? GetStringProperty(object? source, string propertyName)
    {
        object? value = GetPropertyValue(source, propertyName);
        return value as string;
    }

    private static string? GetPropertyText(object? source, string propertyName)
    {
        object? value = GetPropertyValue(source, propertyName);
        return value?.ToString();
    }

    private static bool? GetNullableBooleanProperty(object? source, string propertyName)
    {
        object? value = GetPropertyValue(source, propertyName);
        if (value is bool booleanValue)
        {
            return booleanValue;
        }

        return null;
    }

    private static IReadOnlyCollection<string> GetStringCollectionProperty(object? source, string propertyName)
    {
        object? value = GetPropertyValue(source, propertyName);
        if (value is IEnumerable<string> strings)
        {
            return NormalizeTargets(strings).ToList();
        }

        return Array.Empty<string>();
    }

    private static object? GetPropertyValue(object? source, string propertyName)
    {
        if (source is null)
        {
            return null;
        }

        PropertyInfo? property = source.GetType().GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.IgnoreCase);
        return property?.GetValue(source);
    }

    private static IEnumerable<string> NormalizeTargets(IEnumerable<string> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Distinct(StringComparer.Ordinal);
    }

    private static IEnumerable<string> NormalizePaths(IEnumerable<string> values)
    {
        return values
            .Where(static value => !string.IsNullOrWhiteSpace(value))
            .Select(static value => value.Trim())
            .Select(static value => value.StartsWith("/", StringComparison.Ordinal) ? value : $"/{value}")
            .Distinct(StringComparer.Ordinal);
    }

    private static void AddNonEmpty(ISet<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            values.Add(value.Trim());
        }
    }

    private static void AddRange(ISet<string> values, IEnumerable<string> candidates)
    {
        foreach (string candidate in NormalizeTargets(candidates))
        {
            values.Add(candidate);
        }
    }

    private static TEnum? ParseEnum<TEnum>(string? value)
        where TEnum : struct
    {
        return Enum.TryParse(value, true, out TEnum parsed) ? parsed : null;
    }

    private static string NormalizeEntityType(string entityType)
    {
        return entityType.Trim().Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).ToLowerInvariant();
    }
}
