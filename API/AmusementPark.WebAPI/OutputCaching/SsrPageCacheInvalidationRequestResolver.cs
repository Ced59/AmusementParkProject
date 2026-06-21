using System.Reflection;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Seo.Services;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.WebAPI.Contracts.ContextualBlocks;
using AmusementPark.WebAPI.Contracts.ParkGraphUpserts;
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
            "ParkGraphUpserts" => await this.ResolveParkGraphUpsertAsync(context, executedContext, includeSeoDocuments, cancellationToken),
            "ContextualBlocks" => await this.ResolveContextualBlocksAsync(context, executedContext, includeSeoDocuments, cancellationToken),
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

    private async Task<SsrPageCacheInvalidationRequest> ResolveContextualBlocksAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        object? resultValue = ResolveResultValue(executedContext);
        if (resultValue is not ContextualBlockPreviewResultDto result)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (!result.IsApplied || !result.CanApply || result.Changes.Count == 0)
        {
            return BuildRequest(Array.Empty<string>(), Array.Empty<string>(), includeSeoDocuments: false);
        }

        string? blockType = result.BlockType;
        string? entityId = result.Target.EntityId;

        if (string.IsNullOrWhiteSpace(blockType) || string.IsNullOrWhiteSpace(entityId))
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        if (blockType.Trim().StartsWith("park.", StringComparison.Ordinal))
        {
            return BuildParkImpactRequest(new[] { entityId.Trim() }, includeSeoDocuments, includeDiscoveryPages: true);
        }

        string entityType = NormalizeEntityType(result.Target.EntityType);
        if (string.Equals(entityType, "parkitem", StringComparison.Ordinal))
        {
            HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
            HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);
            bool resolved = await this.AddParkItemImpactAsync(paths, prefixes, entityId.Trim(), cancellationToken);
            return resolved
                ? BuildRequest(paths, prefixes, includeSeoDocuments)
                : SsrPageCacheInvalidationRequest.AllCaches();
        }

        return SsrPageCacheInvalidationRequest.AllCaches();
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

    private async Task<SsrPageCacheInvalidationRequest> ResolveParkGraphUpsertAsync(
        ActionExecutingContext context,
        ActionExecutedContext? executedContext,
        bool includeSeoDocuments,
        CancellationToken cancellationToken)
    {
        if (executedContext is null)
        {
            return BuildRequest(Array.Empty<string>(), Array.Empty<string>(), includeSeoDocuments: false);
        }

        object? resultValue = ResolveResultValue(executedContext);
        if (resultValue is not ParkGraphUpsertResultDto result)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        IReadOnlyCollection<ParkGraphUpsertChangeDto> changedEntities = result.Changes
            .Where(static change => !string.Equals(change.ChangeType, "Unchanged", StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (changedEntities.Count == 0)
        {
            return BuildRequest(Array.Empty<string>(), Array.Empty<string>(), includeSeoDocuments: false);
        }

        if (changedEntities.Count > MaxTargetedEntityCount)
        {
            return SsrPageCacheInvalidationRequest.AllCaches();
        }

        HashSet<string> paths = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> prefixes = new HashSet<string>(StringComparer.Ordinal);
        HashSet<string> fallbackParkIds = new HashSet<string>(StringComparer.Ordinal);
        bool includeDiscoveryPages = false;
        bool requiresHardPurge = false;

        foreach (ParkGraphUpsertChangeDto change in changedEntities)
        {
            requiresHardPurge = requiresHardPurge || ContainsHardPurgeSignal(change);
            string entityType = NormalizeEntityType(change.EntityType);
            string? entityId = NormalizeTarget(change.EntityId);

            if (string.Equals(entityType, "park", StringComparison.Ordinal))
            {
                AddNonEmpty(fallbackParkIds, entityId ?? result.TargetParkId);
                includeDiscoveryPages = true;
                continue;
            }

            if (string.Equals(entityType, "parkitem", StringComparison.Ordinal) || string.Equals(entityType, "attraction", StringComparison.Ordinal))
            {
                bool resolved = await this.AddParkItemImpactAsync(paths, prefixes, entityId, cancellationToken);
                if (!resolved)
                {
                    AddNonEmpty(fallbackParkIds, result.TargetParkId);
                }

                continue;
            }

            if (string.Equals(entityType, "parkzone", StringComparison.Ordinal) || string.Equals(entityType, "zone", StringComparison.Ordinal))
            {
                bool resolved = await this.AddParkZoneImpactAsync(paths, prefixes, entityId, cancellationToken);
                if (!resolved)
                {
                    AddNonEmpty(fallbackParkIds, result.TargetParkId);
                }

                continue;
            }

            if (string.Equals(entityType, "image", StringComparison.Ordinal))
            {
                bool resolved = await this.AddImageImpactAsync(paths, prefixes, change, cancellationToken);
                if (!resolved)
                {
                    AddNonEmpty(fallbackParkIds, result.TargetParkId);
                }

                continue;
            }

            if (string.Equals(entityType, "parkoperator", StringComparison.Ordinal) || string.Equals(entityType, "operator", StringComparison.Ordinal))
            {
                await this.AddReferenceImpactAsync(paths, prefixes, entityId, "operator", id => this.parkRepository.GetParkIdsByOperatorIdAsync(id, cancellationToken));
                continue;
            }

            if (string.Equals(entityType, "parkfounder", StringComparison.Ordinal) || string.Equals(entityType, "founder", StringComparison.Ordinal))
            {
                await this.AddReferenceImpactAsync(paths, prefixes, entityId, "founder", id => this.parkRepository.GetParkIdsByFounderIdAsync(id, cancellationToken));
                continue;
            }

            if (string.Equals(entityType, "attractionmanufacturer", StringComparison.Ordinal) || string.Equals(entityType, "manufacturer", StringComparison.Ordinal))
            {
                await this.AddReferenceImpactAsync(paths, prefixes, entityId, "manufacturer", id => this.parkItemRepository.GetParkIdsByManufacturerIdAsync(id, cancellationToken));
                continue;
            }

            AddNonEmpty(fallbackParkIds, result.TargetParkId);
        }

        AddParkPrefixes(prefixes, fallbackParkIds);

        if (includeDiscoveryPages)
        {
            AddDiscoveryPaths(paths);
        }

        SsrPageCacheInvalidationRequest request = BuildRequest(paths, prefixes, includeSeoDocuments);
        return requiresHardPurge ? ForceHardPurge(request) : request;
    }

    private async Task<bool> AddParkItemImpactAsync(
        ISet<string> paths,
        ISet<string> prefixes,
        string? itemId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return false;
        }

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(itemId, true, cancellationToken);
        if (item is null || string.IsNullOrWhiteSpace(item.ParkId))
        {
            return false;
        }

        Park? park = await this.parkRepository.GetByIdAsync(item.ParkId, true, cancellationToken);
        if (park is null)
        {
            return false;
        }

        AddParkDetailPaths(paths, park);
        AddParkItemListPaths(paths, park);
        AddParkItemPrefixes(prefixes, park, item);

        if (!string.IsNullOrWhiteSpace(item.ZoneId))
        {
            ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(item.ZoneId, cancellationToken);
            if (zone is not null)
            {
                AddParkZoneListPaths(paths, park);
                AddParkZonePrefixes(prefixes, park, zone);
            }
        }

        return true;
    }

    private async Task<bool> AddParkZoneImpactAsync(
        ISet<string> paths,
        ISet<string> prefixes,
        string? zoneId,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(zoneId))
        {
            return false;
        }

        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(zoneId, cancellationToken);
        if (zone is null || string.IsNullOrWhiteSpace(zone.ParkId))
        {
            return false;
        }

        Park? park = await this.parkRepository.GetByIdAsync(zone.ParkId, true, cancellationToken);
        if (park is null)
        {
            return false;
        }

        AddParkDetailPaths(paths, park);
        AddParkItemListPaths(paths, park);
        AddParkZoneListPaths(paths, park);
        AddParkZonePrefixes(prefixes, park, zone);
        return true;
    }

    private async Task<bool> AddImageImpactAsync(
        ISet<string> paths,
        ISet<string> prefixes,
        ParkGraphUpsertChangeDto change,
        CancellationToken cancellationToken)
    {
        HashSet<string> ownerTargets = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        Image? image = string.IsNullOrWhiteSpace(change.EntityId)
            ? null
            : await this.imageRepository.GetByIdAsync(change.EntityId, cancellationToken);

        AddImageOwnerTarget(ownerTargets, image?.OwnerType.ToString(), image?.OwnerId);

        ParkGraphUpsertFieldChangeDto? ownerTypeChange = change.Fields.FirstOrDefault(static field => string.Equals(field.Field, "ownerType", StringComparison.OrdinalIgnoreCase));
        ParkGraphUpsertFieldChangeDto? ownerIdChange = change.Fields.FirstOrDefault(static field => string.Equals(field.Field, "ownerId", StringComparison.OrdinalIgnoreCase));
        string? fallbackOwnerType = image?.OwnerType.ToString();
        AddImageOwnerTarget(ownerTargets, ownerTypeChange?.OldValue ?? fallbackOwnerType, ownerIdChange?.OldValue);
        AddImageOwnerTarget(ownerTargets, ownerTypeChange?.NewValue ?? fallbackOwnerType, ownerIdChange?.NewValue);

        bool resolved = false;
        foreach (string ownerTarget in ownerTargets)
        {
            string[] parts = ownerTarget.Split(':', 2);
            if (parts.Length != 2)
            {
                continue;
            }

            resolved = await this.AddImageOwnerImpactAsync(paths, prefixes, parts[0], parts[1], cancellationToken) || resolved;
        }

        return resolved;
    }

    private async Task<bool> AddImageOwnerImpactAsync(
        ISet<string> paths,
        ISet<string> prefixes,
        string ownerType,
        string ownerId,
        CancellationToken cancellationToken)
    {
        string normalizedOwnerType = NormalizeEntityType(ownerType);
        if (string.Equals(normalizedOwnerType, "park", StringComparison.Ordinal))
        {
            Park? park = await this.parkRepository.GetByIdAsync(ownerId, true, cancellationToken);
            if (park is null)
            {
                return false;
            }

            AddParkDetailPaths(paths, park);
            AddParkImagePaths(paths, park);
            return true;
        }

        if (string.Equals(normalizedOwnerType, "parkitem", StringComparison.Ordinal) || string.Equals(normalizedOwnerType, "attraction", StringComparison.Ordinal))
        {
            return await this.AddParkItemImpactAsync(paths, prefixes, ownerId, cancellationToken);
        }

        return false;
    }

    private async Task AddReferenceImpactAsync(
        ISet<string> paths,
        ISet<string> prefixes,
        string? referenceId,
        string referenceKind,
        Func<string, Task<IReadOnlyCollection<string>>> parkIdsResolver)
    {
        if (string.IsNullOrWhiteSpace(referenceId))
        {
            return;
        }

        AddReferencePrefixes(prefixes, referenceKind, referenceId);
        IReadOnlyCollection<string> parkIds = await parkIdsResolver(referenceId);
        AddParkPrefixes(prefixes, parkIds);
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

    private static void AddParkDetailPaths(ISet<string> paths, Park park)
    {
        foreach (string language in PublicLanguages)
        {
            paths.Add(BuildParkBasePath(language, park));
        }
    }

    private static void AddParkImagePaths(ISet<string> paths, Park park)
    {
        foreach (string language in PublicLanguages)
        {
            paths.Add($"{BuildParkBasePath(language, park)}/images");
        }
    }

    private static void AddParkItemListPaths(ISet<string> paths, Park park)
    {
        foreach (string language in PublicLanguages)
        {
            paths.Add($"{BuildParkBasePath(language, park)}/items");
        }
    }

    private static void AddParkZoneListPaths(ISet<string> paths, Park park)
    {
        foreach (string language in PublicLanguages)
        {
            paths.Add($"{BuildParkBasePath(language, park)}/zones");
        }
    }

    private static void AddParkItemPrefixes(ISet<string> prefixes, Park park, ParkItem item)
    {
        if (string.IsNullOrWhiteSpace(item.Id))
        {
            return;
        }

        foreach (string language in PublicLanguages)
        {
            prefixes.Add($"{BuildParkBasePath(language, park)}/item/{item.Id}/");
        }
    }

    private static void AddParkZonePrefixes(ISet<string> prefixes, Park park, ParkZone zone)
    {
        if (string.IsNullOrWhiteSpace(zone.Id))
        {
            return;
        }

        foreach (string language in PublicLanguages)
        {
            prefixes.Add($"{BuildParkBasePath(language, park)}/zone/{zone.Id}/");
        }
    }

    private static string BuildParkBasePath(string language, Park park)
    {
        string parkSlug = SeoSlugService.ToSlug(park.Name, "park");
        return $"/{language}/park/{park.Id}/{parkSlug}";
    }

    private static void AddImageOwnerTarget(ISet<string> ownerTargets, string? ownerType, string? ownerId)
    {
        if (string.IsNullOrWhiteSpace(ownerType) || string.IsNullOrWhiteSpace(ownerId))
        {
            return;
        }

        ownerTargets.Add($"{ownerType.Trim()}:{ownerId.Trim()}");
    }

    private static bool ContainsHardPurgeSignal(ParkGraphUpsertChangeDto change)
    {
        return change.Fields.Any(static field =>
            (string.Equals(field.Field, "isVisible", StringComparison.OrdinalIgnoreCase) && string.Equals(field.NewValue, "false", StringComparison.OrdinalIgnoreCase))
            || (string.Equals(field.Field, "adminReviewStatus", StringComparison.OrdinalIgnoreCase) && string.Equals(field.NewValue, AdminReviewStatus.NotRelevant.ToString(), StringComparison.OrdinalIgnoreCase)));
    }

    private static SsrPageCacheInvalidationRequest ForceHardPurge(SsrPageCacheInvalidationRequest request)
    {
        return new SsrPageCacheInvalidationRequest
        {
            All = request.All,
            Paths = request.Paths,
            Prefixes = request.Prefixes,
            IncludeSeoDocuments = request.IncludeSeoDocuments,
            AllowStale = false,
            Refresh = false,
        };
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

    private static string? NormalizeTarget(string? value)
    {
        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
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
