using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Application.Features.ParkOpeningHours.Models;
using ParkPricingEntity = AmusementPark.Core.Domain.Parks.ParkPricing;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphUpsertProcessorDeletionSafetyExtensions
{
    internal static void ValidateTargetParkDeletionPreflight(
        this ParkGraphUpsertProcessor processorContext,
        JsonElement root,
        JsonElement? parkPatch,
        Park targetPark,
        ParkGraphUpsertResult result)
    {
        List<ParkGraphDeletionRequest> requests = ParkGraphUpsertProcessorDeletionsExtensions.ReadDeletionRequests(root, result);
        bool deletesTargetPark = requests.Any(request =>
            string.Equals(request.Id, targetPark.Id, StringComparison.Ordinal)
            && (string.IsNullOrWhiteSpace(request.EntityType)
                || string.Equals(
                    ParkGraphUpsertProcessorDeletionsExtensions.NormalizeDeletionEntityType(request.EntityType),
                    "Park",
                    StringComparison.Ordinal)));
        if (!deletesTargetPark)
        {
            return;
        }

        List<string> blockers = new List<string>();
        if (parkPatch.HasValue)
        {
            blockers.Add("le document de suppression ne doit contenir aucune mutation 'park'");
        }

        if (targetPark.IsVisible || targetPark.AdminReviewStatus != AdminReviewStatus.NotRelevant)
        {
            blockers.Add("le parc enregistré doit déjà être masqué et classé NotRelevant avant cette requête");
        }

        if (blockers.Count > 0)
        {
            processorContext.AddSkippedDeletionChange(
                result,
                "Park",
                targetPark.Id,
                $"Suppression Park '{targetPark.Id}' refusée : {string.Join(" ; ", blockers)}.");
        }
    }

    internal static async Task<bool> CanDeleteParkAsync(
        this ParkGraphUpsertProcessor processorContext,
        Park park,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        IReadOnlyCollection<ParkItem> items = await processorContext.parkItemRepository.GetByParkIdAsync(park.Id, true, cancellationToken);
        IReadOnlyCollection<ParkZone> zones = await processorContext.parkZoneRepository.GetByParkIdAsync(park.Id, cancellationToken);
        IReadOnlyCollection<Image> images = await processorContext.imageRepository.GetByOwnersAsync(ImageOwnerType.Park, new[] { park.Id }, null, cancellationToken);
        ParkOpeningHoursSchedule? openingHours = processorContext.parkOpeningHoursRepository is null
            ? null
            : await processorContext.parkOpeningHoursRepository.GetByParkIdAsync(park.Id, cancellationToken);
        ParkPricingEntity? pricing = processorContext.parkPricingRepository is null
            ? null
            : await processorContext.parkPricingRepository.GetByParkIdAsync(park.Id, cancellationToken);
        IReadOnlyCollection<HistoryEvent> historyEvents = processorContext.historyEventRepository is null
            ? Array.Empty<HistoryEvent>()
            : await processorContext.historyEventRepository.GetOwnerTimelineAsync(HistoryEntityType.Park, park.Id, true, cancellationToken);
        long? commentCount = processorContext.commentRepository is null
            ? null
            : await processorContext.commentRepository.CountByTargetAsync(CommentTargetType.Park, park.Id, cancellationToken);

        List<string> dependencies = new List<string>();
        if (items.Count > 0)
        {
            dependencies.Add($"{items.Count} parkItem(s)");
        }

        if (zones.Count > 0)
        {
            dependencies.Add($"{zones.Count} zone(s)");
        }

        if (images.Count > 0)
        {
            dependencies.Add($"{images.Count} image(s) de parc");
        }

        if (park.OfficialMaps.Count > 0)
        {
            dependencies.Add($"{park.OfficialMaps.Count} carte(s) officielle(s)");
        }

        if (openingHours is not null)
        {
            dependencies.Add("des horaires");
        }

        if (pricing is not null)
        {
            dependencies.Add("une tarification");
        }

        if (historyEvents.Count > 0)
        {
            dependencies.Add($"{historyEvents.Count} événement(s) historique(s)");
        }

        if (!commentCount.HasValue)
        {
            dependencies.Add("la vérification des commentaires est indisponible");
        }
        else if (commentCount.Value > 0)
        {
            dependencies.Add($"{commentCount.Value} commentaire(s)");
        }

        if (dependencies.Count == 0)
        {
            return true;
        }

        processorContext.AddSkippedDeletionChange(
            result,
            "Park",
            park.Id,
            $"Suppression Park '{park.Id}' refusée : retirer d'abord les dépendances contrôlées suivantes : {string.Join(", ", dependencies)}.");
        return false;
    }

    internal static async Task<bool> CanDeleteParkItemAsync(
        this ParkGraphUpsertProcessor processorContext,
        ParkItem item,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        long? commentCount = processorContext.commentRepository is null
            ? null
            : await processorContext.commentRepository.CountByTargetAsync(CommentTargetType.ParkItem, item.Id, cancellationToken);
        if (commentCount == 0)
        {
            return true;
        }

        string dependency = commentCount.HasValue
            ? $"{commentCount.Value} commentaire(s)"
            : "la vérification des commentaires est indisponible";
        processorContext.AddSkippedDeletionChange(
            result,
            "ParkItem",
            item.Id,
            $"Suppression ParkItem '{item.Id}' refusée : retirer d'abord la dépendance contrôlée suivante : {dependency}.");
        return false;
    }
}
