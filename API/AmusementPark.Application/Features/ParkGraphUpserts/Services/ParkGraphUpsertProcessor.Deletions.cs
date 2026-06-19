using System.Text.Json;
using AmusementPark.Application.Features.Search;
using AmusementPark.Application.Features.Seo.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task<ParkGraphUpsertItemSeoChanges> ProcessDeletionsAsync(JsonElement root, Park targetPark, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        ParkGraphUpsertItemSeoChanges seoChanges = new ParkGraphUpsertItemSeoChanges();
        List<ParkGraphDeletionRequest> requests = ReadDeletionRequests(root, result);
        if (requests.Count == 0)
        {
            return seoChanges;
        }

        foreach (ParkGraphDeletionRequest request in requests)
        {
            ParkGraphDeletionTarget? target = await this.ResolveDeletionTargetAsync(request, result, cancellationToken);
            if (target is null)
            {
                continue;
            }

            ParkGraphUpsertChange change = BuildEntityChange(target.EntityType, target.Id, null, target.DisplayName, "Deleted", request.MatchedBy);
            AddChange(change, "suppr", "present", "deleted");
            result.Changes.Add(change);

            if (!apply)
            {
                continue;
            }

            bool deleted = await this.DeleteTargetAsync(target, targetPark, seoChanges, cancellationToken);
            if (!deleted)
            {
                change.ChangeType = "Skipped";
                result.Errors.Add($"Suppression {target.EntityType} '{target.Id}' impossible : l'élément n'a pas été supprimé.");
            }
        }

        return seoChanges;
    }

    private async Task<ParkGraphDeletionTarget?> ResolveDeletionTargetAsync(ParkGraphDeletionRequest request, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Id))
        {
            this.AddSkippedDeletionChange(result, request.EntityType ?? "Unknown", string.Empty, "Suppression ignorée : un identifiant est requis.");
            return null;
        }

        string id = request.Id.Trim();
        string? normalizedEntityType = NormalizeDeletionEntityType(request.EntityType);
        if (string.IsNullOrWhiteSpace(normalizedEntityType))
        {
            return await this.ResolveDeletionTargetByIdAsync(id, request, result, cancellationToken);
        }

        if (string.Equals(normalizedEntityType, "Image", StringComparison.Ordinal))
        {
            Image? image = await this.imageRepository.GetByIdAsync(id, cancellationToken);
            if (image is null)
            {
                this.AddSkippedDeletionChange(result, "Image", id, $"Suppression Image '{id}' impossible : image introuvable.");
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
            ParkItem? item = await this.parkItemRepository.GetByIdAsync(id, true, cancellationToken);
            if (item is null)
            {
                this.AddSkippedDeletionChange(result, "ParkItem", id, $"Suppression ParkItem '{id}' impossible : élément introuvable.");
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
            ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(id, cancellationToken);
            if (zone is null)
            {
                this.AddSkippedDeletionChange(result, "ParkZone", id, $"Suppression ParkZone '{id}' impossible : zone introuvable.");
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

        this.AddSkippedDeletionChange(result, request.EntityType ?? "Unknown", id, $"Suppression '{request.EntityType}' impossible : type non pris en charge par le JSON upsert.");
        return null;
    }

    private async Task<ParkGraphDeletionTarget?> ResolveDeletionTargetByIdAsync(string id, ParkGraphDeletionRequest request, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        Image? image = await this.imageRepository.GetByIdAsync(id, cancellationToken);
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

        ParkItem? item = await this.parkItemRepository.GetByIdAsync(id, true, cancellationToken);
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

        ParkZone? zone = await this.parkZoneRepository.GetByIdAsync(id, cancellationToken);
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

        this.AddSkippedDeletionChange(result, "Unknown", id, $"Suppression '{id}' impossible : aucun élément compatible trouvé.");
        return null;
    }

    private async Task<bool> DeleteTargetAsync(ParkGraphDeletionTarget target, Park targetPark, ParkGraphUpsertItemSeoChanges seoChanges, CancellationToken cancellationToken)
    {
        if (target.Image is not null)
        {
            bool deleted = await this.imageRepository.DeleteAsync(target.Image.Id, cancellationToken);
            if (deleted)
            {
                await this.SynchronizeDeletedImageAsync(target.Image, targetPark, cancellationToken);
            }

            return deleted;
        }

        if (target.ParkItem is not null)
        {
            PublicSeoParkItemSnapshot? previousSnapshot = PublicSeoParkItemSnapshot.FromParkItem(target.ParkItem);
            bool deleted = await this.parkItemRepository.DeleteAsync(target.ParkItem.Id, cancellationToken);
            if (!deleted)
            {
                return false;
            }

            await this.searchProjectionWriter.DeleteAsync(SearchProjectionResourceTypes.ParkItems, target.ParkItem.Id, cancellationToken);
            if (previousSnapshot is not null)
            {
                seoChanges.PreviousItems.Add(previousSnapshot);
            }

            return true;
        }

        if (target.ParkZone is not null)
        {
            return await this.parkZoneRepository.DeleteAsync(target.ParkZone.Id, cancellationToken);
        }

        return false;
    }

    private async Task SynchronizeDeletedImageAsync(Image image, Park targetPark, CancellationToken cancellationToken)
    {
        if (image.OwnerType != ImageOwnerType.Park
            || image.Category != ImageCategory.ParkLogo
            || string.IsNullOrWhiteSpace(image.OwnerId))
        {
            return;
        }

        Park? ownerPark = string.Equals(image.OwnerId, targetPark.Id, StringComparison.Ordinal)
            ? targetPark
            : await this.parkRepository.GetByIdAsync(image.OwnerId, true, cancellationToken);
        if (ownerPark is null)
        {
            return;
        }

        Image? currentLogo = await this.imageRepository.GetCurrentByOwnerAsync(ImageOwnerType.Park, image.OwnerId, ImageCategory.ParkLogo, cancellationToken);
        ownerPark.CurrentLogoImageId = currentLogo?.Id;
        await this.parkRepository.UpdateAsync(ownerPark.Id, ownerPark, cancellationToken);
    }

    private void AddSkippedDeletionChange(ParkGraphUpsertResult result, string entityType, string id, string message)
    {
        result.Errors.Add(message);
        ParkGraphUpsertChange change = BuildEntityChange(entityType, id, null, string.IsNullOrWhiteSpace(id) ? entityType : id, "Skipped", "suppr");
        AddChange(change, "suppr", "present", "skipped");
        result.Changes.Add(change);
    }

    private static List<ParkGraphDeletionRequest> ReadDeletionRequests(JsonElement root, ParkGraphUpsertResult result)
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
                AddDeletionRequest(item, requests, result);
            }

            return requests;
        }

        if (suppr.ValueKind == JsonValueKind.Object)
        {
            AddDeletionRequest(suppr, requests, result);
            AddDeletionRequestsFromArray(suppr, "images", "Image", requests, result);
            AddDeletionRequestsFromArray(suppr, "imageIds", "Image", requests, result);
            AddDeletionRequestsFromArray(suppr, "items", "ParkItem", requests, result);
            AddDeletionRequestsFromArray(suppr, "itemIds", "ParkItem", requests, result);
            AddDeletionRequestsFromArray(suppr, "parkItems", "ParkItem", requests, result);
            AddDeletionRequestsFromArray(suppr, "parkItemIds", "ParkItem", requests, result);
            AddDeletionRequestsFromArray(suppr, "zones", "ParkZone", requests, result);
            AddDeletionRequestsFromArray(suppr, "zoneIds", "ParkZone", requests, result);
            AddDeletionRequestsFromArray(suppr, "parkZones", "ParkZone", requests, result);
            AddDeletionRequestsFromArray(suppr, "parkZoneIds", "ParkZone", requests, result);
            return requests;
        }

        result.Errors.Add("Le paramètre suppr doit être un tableau ou un objet.");
        return requests;
    }

    private static void AddDeletionRequestsFromArray(JsonElement owner, string propertyName, string entityType, List<ParkGraphDeletionRequest> requests, ParkGraphUpsertResult result)
    {
        if (!owner.TryGetProperty(propertyName, out JsonElement array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (JsonElement item in array.EnumerateArray())
        {
            if (item.ValueKind == JsonValueKind.String)
            {
                requests.Add(new ParkGraphDeletionRequest(entityType, NormalizeString(item.GetString()), $"suppr.{propertyName}"));
            }
            else if (item.ValueKind == JsonValueKind.Object)
            {
                string? id = ReadString(item, "id") ?? ReadTypedDeletionId(item, entityType);
                requests.Add(new ParkGraphDeletionRequest(entityType, id, $"suppr.{propertyName}"));
            }
            else
            {
                result.Errors.Add($"Entrée suppr.{propertyName} ignorée : l'identifiant doit être une chaîne ou un objet.");
            }
        }
    }

    private static void AddDeletionRequest(JsonElement item, List<ParkGraphDeletionRequest> requests, ParkGraphUpsertResult result)
    {
        if (item.ValueKind == JsonValueKind.String)
        {
            requests.Add(new ParkGraphDeletionRequest(null, NormalizeString(item.GetString()), "suppr.id"));
            return;
        }

        if (item.ValueKind != JsonValueKind.Object)
        {
            result.Errors.Add("Entrée suppr ignorée : l'identifiant doit être une chaîne ou un objet.");
            return;
        }

        string? entityType = ReadString(item, "entityType") ?? ReadString(item, "type") ?? ReadString(item, "kind");
        string? id = ReadString(item, "id");
        string matchedBy = "suppr.id";

        if (string.IsNullOrWhiteSpace(id))
        {
            id = ReadString(item, "imageId");
            if (!string.IsNullOrWhiteSpace(id))
            {
                entityType ??= "Image";
                matchedBy = "suppr.imageId";
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            id = ReadString(item, "parkItemId") ?? ReadString(item, "itemId");
            if (!string.IsNullOrWhiteSpace(id))
            {
                entityType ??= "ParkItem";
                matchedBy = "suppr.parkItemId";
            }
        }

        if (string.IsNullOrWhiteSpace(id))
        {
            id = ReadString(item, "parkZoneId") ?? ReadString(item, "zoneId");
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

    private static string? ReadTypedDeletionId(JsonElement item, string entityType)
    {
        if (string.Equals(entityType, "Image", StringComparison.Ordinal))
        {
            return ReadString(item, "imageId");
        }

        if (string.Equals(entityType, "ParkItem", StringComparison.Ordinal))
        {
            return ReadString(item, "parkItemId") ?? ReadString(item, "itemId");
        }

        if (string.Equals(entityType, "ParkZone", StringComparison.Ordinal))
        {
            return ReadString(item, "parkZoneId") ?? ReadString(item, "zoneId");
        }

        return null;
    }

    private static string? NormalizeDeletionEntityType(string? entityType)
    {
        if (string.IsNullOrWhiteSpace(entityType))
        {
            return null;
        }

        string normalized = entityType
            .Replace("-", string.Empty, StringComparison.Ordinal)
            .Replace("_", string.Empty, StringComparison.Ordinal)
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .Trim()
            .ToLowerInvariant();

        return normalized switch
        {
            "image" or "images" => "Image",
            "parkitem" or "parkitems" or "item" or "items" or "attraction" or "attractions" => "ParkItem",
            "parkzone" or "parkzones" or "zone" or "zones" => "ParkZone",
            _ => entityType.Trim(),
        };
    }

    private sealed record ParkGraphDeletionRequest(string? EntityType, string? Id, string MatchedBy);

    private sealed class ParkGraphDeletionTarget
    {
        public string EntityType { get; init; } = string.Empty;

        public string Id { get; init; } = string.Empty;

        public string DisplayName { get; init; } = string.Empty;

        public Image? Image { get; init; }

        public ParkItem? ParkItem { get; init; }

        public ParkZone? ParkZone { get; init; }
    }
}
