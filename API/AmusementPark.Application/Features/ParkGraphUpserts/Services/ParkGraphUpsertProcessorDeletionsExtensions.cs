using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using System.Globalization;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Localization;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Contracts;
using AmusementPark.Application.Features.ParkGraphUpserts.Ports;
using AmusementPark.Core.Geo;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.ParkFounders.Ports;
using AmusementPark.Application.Features.ParkGraphUpserts;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.ParkOperators.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.ParkZones.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Parks.Services;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;
using System.Text;
using AmusementPark.Application.Common.Measurements;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Ports;
using AmusementPark.Application.Features.ParkOpeningHours.Services;
using AmusementPark.Application.Features.ParkPricing.Ports;
using AmusementPark.Application.Features.ParkPricing.Services;
using AmusementPark.Application.Features.Seo.Ports;
using AmusementPark.Application.Features.SocialPublishing.Ports;
using AmusementPark.Application.Features.StandaloneAttractions.Ports;
using AmusementPark.Core.Domain.SocialPublishing;
using AmusementPark.Application.Features.Parks.Contracts;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;
internal static class ParkGraphUpsertProcessorDeletionsExtensions
{
    internal static async Task<ParkGraphUpsertItemSeoChanges> ProcessDeletionsAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, Park targetPark, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertItemSeoChanges seoChanges = new ParkGraphUpsertItemSeoChanges();
        List<ParkGraphDeletionRequest> requests = ParkGraphUpsertProcessorDeletionsExtensions.ReadDeletionRequests(root, result);
        if (requests.Count == 0)
        {
            return seoChanges;
        }

        List<ParkGraphResolvedDeletion> resolvedDeletions = new List<ParkGraphResolvedDeletion>();
        HashSet<string> resolvedTargetKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ParkGraphDeletionRequest request in requests)
        {
            ParkGraphDeletionTarget? target = await processorContext.ResolveDeletionTargetAsync(request, result, cancellationToken);
            if (target is null)
            {
                continue;
            }

            if (!await processorContext.IsDeletionTargetInTargetParkAsync(target, targetPark, cancellationToken))
            {
                processorContext.AddSkippedDeletionChange(result, target.EntityType, target.Id, $"Suppression {target.EntityType} '{target.Id}' refusée : l'élément n'appartient pas au parc cible '{targetPark.Id}'.");
                continue;
            }

            string targetKey = $"{target.EntityType}:{target.Id}";
            if (!resolvedTargetKeys.Add(targetKey))
            {
                processorContext.AddSkippedDeletionChange(result, target.EntityType, target.Id, $"Suppression {target.EntityType} '{target.Id}' refusée : la cible apparaît plusieurs fois dans le lot.");
                continue;
            }

            resolvedDeletions.Add(new ParkGraphResolvedDeletion(request, target));
        }

        if (result.Errors.Count > 0 || resolvedDeletions.Count != requests.Count)
        {
            return seoChanges;
        }

        foreach (ParkGraphResolvedDeletion deletion in resolvedDeletions)
        {
            ParkGraphDeletionRequest request = deletion.Request;
            ParkGraphDeletionTarget target = deletion.Target;
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(target.EntityType, target.Id, null, target.DisplayName, "Deleted", request.MatchedBy);
            ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "suppr", "present", "deleted");
            result.Changes.Add(change);
            if (!apply)
            {
                continue;
            }

            bool deleted = await processorContext.DeleteTargetAsync(target, targetPark, seoChanges, cancellationToken);
            if (!deleted)
            {
                change.ChangeType = "Skipped";
                result.Errors.Add($"Suppression {target.EntityType} '{target.Id}' impossible : l'élément n'a pas été supprimé.");
                break;
            }
        }

        return seoChanges;
    }

    internal static async Task<ParkGraphDeletionTarget?> ResolveDeletionTargetAsync(this ParkGraphUpsertProcessor processorContext, ParkGraphDeletionRequest request, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            processorContext.AddSkippedDeletionChange(result, request.EntityType ?? "Unknown", string.Empty, "Suppression ignorée : un identifiant est requis.");
            return null;
        }

        string id = request.Id.Trim();
        string? normalizedEntityType = ParkGraphUpsertProcessorDeletionsExtensions.NormalizeDeletionEntityType(request.EntityType);
        if (string.IsNullOrWhiteSpace(normalizedEntityType))
        {
            return await processorContext.ResolveDeletionTargetByIdAsync(id, request, result, cancellationToken);
        }

        if (string.Equals(normalizedEntityType, "Image", StringComparison.Ordinal))
        {
            Image? image = await processorContext.imageRepository.GetByIdAsync(id, cancellationToken);
            if (image is null)
            {
                processorContext.AddSkippedDeletionChange(result, "Image", id, $"Suppression Image '{id}' impossible : image introuvable.");
                return null;
            }

            return new ParkGraphDeletionTarget
            {
                EntityType = "Image",
                Id = image.Id,
                DisplayName = image.OriginalFileName ?? image.Description ?? image.Id,
                Image = image,
            };
        }

        if (string.Equals(normalizedEntityType, "ParkItem", StringComparison.Ordinal))
        {
            ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(id, true, cancellationToken);
            if (item is null)
            {
                processorContext.AddSkippedDeletionChange(result, "ParkItem", id, $"Suppression ParkItem '{id}' impossible : élément introuvable.");
                return null;
            }

            return new ParkGraphDeletionTarget
            {
                EntityType = "ParkItem",
                Id = item.Id,
                DisplayName = item.Name,
                ParkItem = item,
            };
        }

        if (string.Equals(normalizedEntityType, "ParkZone", StringComparison.Ordinal))
        {
            ParkZone? zone = await processorContext.parkZoneRepository.GetByIdAsync(id, cancellationToken);
            if (zone is null)
            {
                processorContext.AddSkippedDeletionChange(result, "ParkZone", id, $"Suppression ParkZone '{id}' impossible : zone introuvable.");
                return null;
            }

            return new ParkGraphDeletionTarget
            {
                EntityType = "ParkZone",
                Id = zone.Id,
                DisplayName = zone.Name,
                ParkZone = zone,
            };
        }

        processorContext.AddSkippedDeletionChange(result, request.EntityType ?? "Unknown", id, $"Suppression '{request.EntityType}' impossible : type non pris en charge par le JSON upsert.");
        return null;
    }

    internal static async Task<ParkGraphDeletionTarget?> ResolveDeletionTargetByIdAsync(this ParkGraphUpsertProcessor processorContext, string id, ParkGraphDeletionRequest request, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        Image? image = await processorContext.imageRepository.GetByIdAsync(id, cancellationToken);
        if (image is not null)
        {
            return new ParkGraphDeletionTarget
            {
                EntityType = "Image",
                Id = image.Id,
                DisplayName = image.OriginalFileName ?? image.Description ?? image.Id,
                Image = image,
            };
        }

        ParkItem? item = await processorContext.parkItemRepository.GetByIdAsync(id, true, cancellationToken);
        if (item is not null)
        {
            return new ParkGraphDeletionTarget
            {
                EntityType = "ParkItem",
                Id = item.Id,
                DisplayName = item.Name,
                ParkItem = item,
            };
        }

        ParkZone? zone = await processorContext.parkZoneRepository.GetByIdAsync(id, cancellationToken);
        if (zone is not null)
        {
            return new ParkGraphDeletionTarget
            {
                EntityType = "ParkZone",
                Id = zone.Id,
                DisplayName = zone.Name,
                ParkZone = zone,
            };
        }

        processorContext.AddSkippedDeletionChange(result, "Unknown", id, $"Suppression '{id}' impossible : aucun élément compatible trouvé.");
        return null;
    }

    internal static async Task<bool> DeleteTargetAsync(this ParkGraphUpsertProcessor processorContext, ParkGraphDeletionTarget target, Park targetPark, ParkGraphUpsertItemSeoChanges seoChanges, CancellationToken cancellationToken)
    {
        if (target.Image is not null)
        {
            if (!string.IsNullOrWhiteSpace(target.Image.Path))
            {
                if (processorContext.imageBinaryStorage is null)
                {
                    return false;
                }

                bool binaryDeleted = await processorContext.imageBinaryStorage.DeleteAsync(target.Image.Path, cancellationToken);
                if (!binaryDeleted)
                {
                    return false;
                }
            }

            bool deleted = await processorContext.imageRepository.DeleteAsync(target.Image.Id, cancellationToken);
            if (deleted)
            {
                await processorContext.SynchronizeDeletedImageAsync(target.Image, targetPark, cancellationToken);
            }

            return deleted;
        }

        if (target.ParkItem is not null)
        {
            PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(target.ParkItem);
            bool deleted = await processorContext.parkItemRepository.DeleteAsync(target.ParkItem.Id, cancellationToken);
            if (!deleted)
            {
                return false;
            }

            await processorContext.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, target.ParkItem.Id, cancellationToken);
            if (previousSnapshot is not null)
            {
                seoChanges.PreviousItems.Add(previousSnapshot);
            }

            return true;
        }

        if (target.ParkZone is not null)
        {
            return await processorContext.parkZoneRepository.DeleteAsync(target.ParkZone.Id, cancellationToken);
        }

        return false;
    }

    internal static async Task<bool> IsDeletionTargetInTargetParkAsync(this ParkGraphUpsertProcessor processorContext, ParkGraphDeletionTarget target, Park targetPark, CancellationToken cancellationToken)
    {
        if (target.ParkItem is not null)
        {
            return string.Equals(target.ParkItem.ParkId, targetPark.Id, StringComparison.Ordinal);
        }

        if (target.ParkZone is not null)
        {
            return string.Equals(target.ParkZone.ParkId, targetPark.Id, StringComparison.Ordinal);
        }

        if (target.Image is not null)
        {
            return await processorContext.IsImageDeletionTargetInTargetParkAsync(target.Image, targetPark, cancellationToken);
        }

        return false;
    }

    internal static async Task<bool> IsImageDeletionTargetInTargetParkAsync(this ParkGraphUpsertProcessor processorContext, Image image, Park targetPark, CancellationToken cancellationToken)
    {
        if (image.OwnerType == ImageOwnerType.Park)
        {
            return string.Equals(image.OwnerId, targetPark.Id, StringComparison.Ordinal);
        }

        if (image.OwnerType == ImageOwnerType.ParkItem && !string.IsNullOrWhiteSpace(image.OwnerId))
        {
            ParkItem? ownerItem = await processorContext.parkItemRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            return string.Equals(ownerItem?.ParkId, targetPark.Id, StringComparison.Ordinal);
        }

        return false;
    }

    internal static async Task SynchronizeDeletedImageAsync(this ParkGraphUpsertProcessor processorContext, Image image, Park targetPark, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(image.OwnerId))
        {
            return;
        }

        if (image.OwnerType == ImageOwnerType.Park && image.Category == ImageCategory.Logo)
        {
            Park? ownerPark = string.Equals(image.OwnerId, targetPark.Id, StringComparison.Ordinal) ? targetPark : await processorContext.parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
            if (ownerPark is null)
            {
                return;
            }

            Image? currentLogo = await processorContext.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, image.OwnerId, ImageCategory.Logo, cancellationToken);
            ownerPark.CurrentLogoImageId = currentLogo?.Id;
            await processorContext.parkRepository.UpdateAsync(ownerPark.Id, ownerPark, cancellationToken);
            return;
        }

        if (image.OwnerType == ImageOwnerType.AttractionManufacturer && image.Category == ImageCategory.Logo)
        {
            AttractionManufacturer? manufacturer = await processorContext.attractionManufacturerRepository.GetByIdAsync(image.OwnerId, cancellationToken);
            if (manufacturer is null)
            {
                return;
            }

            Image? currentLogo = await processorContext.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.AttractionManufacturer, image.OwnerId, ImageCategory.Logo, cancellationToken);
            manufacturer.CurrentLogoImageId = currentLogo?.Id;
            await processorContext.attractionManufacturerRepository.UpdateAsync(manufacturer.Id, manufacturer, cancellationToken);
            await processorContext.searchProjectionWriter.UpsertAsync(SearchProjectionResourceTypes.Manufacturers, manufacturer.Id, cancellationToken);
        }
    }

    internal static void AddSkippedDeletionChange(this ParkGraphUpsertProcessor processorContext, ParkGraphUpsertResult result, string entityType, string id, string message)
    {
        result.Errors.Add(message);
        ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange(entityType, id, null, string.IsNullOrWhiteSpace(id) ? entityType : id, "Skipped", "suppr");
        ParkGraphUpsertProcessorResolutionExtensions.AddChange(change, "suppr", "present", "skipped");
        result.Changes.Add(change);
    }

    internal static List<ParkGraphDeletionRequest> ReadDeletionRequests(JsonElement root, ParkGraphUpsertResult result)
    {
        List<ParkGraphDeletionRequest> requests = new List<ParkGraphDeletionRequest>();
        if (!root.TryGetProperty("suppr", out JsonElement suppr))
        {
            return requests;
        }

        if (suppr.ValueKind == JsonValueKind.Array)
        {
            foreach (JsonElement item in suppr.EnumerateArray())
            {
                ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequest(item, requests, result);
            }

            return requests;
        }

        if (suppr.ValueKind == JsonValueKind.Object)
        {
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequest(suppr, requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "images", "Image", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "imageIds", "Image", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "items", "ParkItem", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "itemIds", "ParkItem", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "parkItems", "ParkItem", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "parkItemIds", "ParkItem", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "zones", "ParkZone", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "zoneIds", "ParkZone", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "parkZones", "ParkZone", requests, result);
            ParkGraphUpsertProcessorDeletionsExtensions.AddDeletionRequestsFromArray(suppr, "parkZoneIds", "ParkZone", requests, result);
            return requests;
        }

        result.Errors.Add("Le paramètre suppr doit être un tableau ou un objet.");
        return requests;
    }

    internal static void AddDeletionRequestsFromArray(JsonElement owner, string propertyName, string entityType, List<ParkGraphDeletionRequest> requests, ParkGraphUpsertResult result)
    {
        if (!owner.TryGetProperty(propertyName, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                requests.Add(new ParkGraphDeletionRequest(entityType, ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(item.GetString()), $"suppr.{propertyName}"));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "id") ?? ParkGraphUpsertProcessorDeletionsExtensions.ReadTypedDeletionId(item, entityType);
                requests.Add(new ParkGraphDeletionRequest(entityType, id, $"suppr.{propertyName}"));
            }
            else
            {
                result.Errors.Add($"Entrée suppr.{propertyName} ignorée : l'identifiant doit être une chaîne ou un objet.");
            }
        }
    }

    internal static void AddDeletionRequest(JsonElement item, List<ParkGraphDeletionRequest> requests, ParkGraphUpsertResult result)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            requests.Add(new ParkGraphDeletionRequest(null, ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(item.GetString()), "suppr.id"));
            return;
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add("Entrée suppr ignorée : l'identifiant doit être une chaîne ou un objet.");
            return;
        }

        string? entityType = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "entityType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "type") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "kind");
        string? id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "id");
        string matchedBy = "suppr.id";
        if (string.IsNullOrWhiteSpace(id))
        {
            id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "imageId");
            if (!string.IsNullOrWhiteSpace(id))
            {
                entityType ??= "Image";
                matchedBy = "suppr.imageId";
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "parkItemId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "itemId");
            if (!string.IsNullOrWhiteSpace(id))
            {
                entityType ??= "ParkItem";
                matchedBy = "suppr.parkItemId";
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            id = ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "parkZoneId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "zoneId");
            if (!string.IsNullOrWhiteSpace(id))
            {
                entityType ??= "ParkZone";
                matchedBy = "suppr.parkZoneId";
            }
        }

        if (string.IsNullOrWhiteSpace(id) && string.IsNullOrWhiteSpace(entityType))
        {
            return;
        }

        requests.Add(new ParkGraphDeletionRequest(entityType, id, matchedBy));
    }

    internal static string? ReadTypedDeletionId(JsonElement item, string entityType)
    {
        if (string.Equals(entityType, "Image", StringComparison.Ordinal))
        {
            return ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "imageId");
        }

        if (string.Equals(entityType, "ParkItem", StringComparison.Ordinal))
        {
            return ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "parkItemId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "itemId");
        }

        if (string.Equals(entityType, "ParkZone", StringComparison.Ordinal))
        {
            return ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "parkZoneId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(item, "zoneId");
        }

        return null;
    }

    internal static string? NormalizeDeletionEntityType(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        string normalized = entityType.Replace("-", string.Empty, StringComparison.Ordinal).Replace("_", string.Empty, StringComparison.Ordinal).Replace(" ", string.Empty, StringComparison.Ordinal).Trim().ToLowerInvariant();
        return normalized switch
        {
            "image" or "images" => "Image",
            "parkitem" or "parkitems" or "item" or "items" or "attraction" or "attractions" => "ParkItem",
            "parkzone" or "parkzones" or "zone" or "zones" => "ParkZone",
            _ => entityType.Trim(),
        };
    }
}
