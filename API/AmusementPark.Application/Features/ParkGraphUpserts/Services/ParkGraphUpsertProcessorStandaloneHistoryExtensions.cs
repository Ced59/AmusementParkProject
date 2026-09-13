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
internal static class ParkGraphUpsertProcessorStandaloneHistoryExtensions
{
    internal static async Task PreflightStandaloneHistoryAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, bool createIfMissing, ParkGraphUpsertResult result, CancellationToken cancellationToken)
    {
        if (ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEvents(root)is null)
        {
            return;
        }

        if (processorContext.standaloneAttractionRepository is null)
        {
            result.Errors.Add("Le repository des attractions autonomes n'est pas configure.");
            return;
        }

        if (processorContext.historyEventRepository is null)
        {
            result.Errors.Add("La section history ne peut pas etre appliquee car le repository d'historique n'est pas disponible.");
            return;
        }

        JsonElement? patch = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "standaloneAttraction");
        JsonElement? identity = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "identity");
        JsonElement? migration = ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "migration") ?? ParkGraphUpsertProcessorJsonReadingExtensions.GetObject(root, "standaloneAttractionMigration");
        StandaloneAttraction? attraction = await processorContext.ResolveStandaloneAttractionAsync(patch, identity, migration, createIfMissing, processorContext.standaloneAttractionRepository, result, cancellationToken);
        if (attraction is null)
        {
            return;
        }

        ParkGraphUpsertProcessorStandaloneHistoryExtensions.ValidateStandaloneHistoryEvents(root, attraction.Id, result);
    }

    internal static void ValidateStandaloneHistoryEvents(JsonElement root, string? targetStandaloneAttractionId, ParkGraphUpsertResult result)
    {
        JsonElement? events = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEvents(root);
        if (events is null)
        {
            return;
        }

        string? resolvedTargetId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(targetStandaloneAttractionId);
        if (string.IsNullOrWhiteSpace(resolvedTargetId))
        {
            result.Errors.Add("Impossible de resoudre l'attraction autonome cible pour la section history.");
            return;
        }

        foreach (JsonElement patch in events.Value.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                result.Errors.Add("Chaque evenement history doit etre un objet JSON.");
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key"));
            string? eventType = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "eventType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                result.Errors.Add("Un evenement history doit definir eventType.");
                continue;
            }

            HistoryEntityType entityType = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEntityType(patch);
            if (entityType != HistoryEntityType.StandaloneAttraction)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' d'un standaloneAttractionGraph doit cibler 'StandaloneAttraction'.");
                continue;
            }

            string ownerId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "standaloneAttractionId")) ?? resolvedTargetId;
            if (!string.Equals(ownerId, resolvedTargetId, StringComparison.Ordinal))
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' cible l'attraction autonome '{ownerId}' au lieu de la cible '{resolvedTargetId}'.");
                continue;
            }

            if (!ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryEventType(entityType, eventType))
            {
                result.Errors.Add($"Le type d'evenement history '{eventType}' n'est pas valide pour '{entityType}'.");
                continue;
            }

            if (ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryDate(patch)is null)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' doit definir une date valide.");
            }
        }
    }

    internal static async Task<bool> ProcessStandaloneHistoryEventsAsync(this ParkGraphUpsertProcessor processorContext, JsonElement root, string? targetStandaloneAttractionId, Dictionary<string, string> imageKeys, ParkGraphUpsertResult result, bool apply, CancellationToken cancellationToken)
    {
        JsonElement? events = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEvents(root);
        if (events is null)
        {
            return false;
        }

        if (processorContext.historyEventRepository is null)
        {
            result.Errors.Add("La section history ne peut pas etre traitee car le repository d'historique n'est pas disponible.");
            return false;
        }

        string? resolvedTargetId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(targetStandaloneAttractionId);
        if (string.IsNullOrWhiteSpace(resolvedTargetId))
        {
            result.Errors.Add("Impossible de resoudre l'attraction autonome cible pour la section history.");
            return false;
        }

        bool changed = false;
        foreach (JsonElement patch in events.Value.EnumerateArray())
        {
            if (patch.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            string? key = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "key"));
            string? eventType = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "eventType") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                continue;
            }

            HistoryEntityType entityType = ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryEntityType(patch);
            if (entityType != HistoryEntityType.StandaloneAttraction)
            {
                continue;
            }

            string ownerId = ParkGraphUpsertProcessorJsonReadingExtensions.NormalizeString(ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "ownerId") ?? ParkGraphUpsertProcessorJsonReadingExtensions.ReadString(patch, "standaloneAttractionId")) ?? resolvedTargetId;
            if (!string.Equals(ownerId, resolvedTargetId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!ParkGraphUpsertProcessorHistoryExtensions.IsValidHistoryEventType(entityType, eventType))
            {
                continue;
            }

            HistoryDateParts? dateParts = ParkGraphUpsertProcessorHistoryExtensions.ReadHistoryDate(patch);
            if (dateParts is null)
            {
                continue;
            }

            key ??= ParkGraphUpsertProcessorHistoryExtensions.BuildHistoryKey(entityType, ownerId, eventType, dateParts);
            HistoryEvent? existing = await processorContext.historyEventRepository.GetByOwnerKeyAsync(entityType, ownerId, key, cancellationToken);
            HistoryEvent historyEvent = existing ?? new HistoryEvent();
            ParkGraphUpsertChange change = ParkGraphUpsertProcessorResolutionExtensions.BuildEntityChange("HistoryEvent", historyEvent.Id, key, ParkGraphUpsertProcessorHistoryExtensions.ResolveHistoryDisplayName(patch, eventType), existing is null ? "Created" : "Unchanged", existing is null ? "key" : "ownerKey");
            ParkGraphUpsertProcessorHistoryExtensions.PatchHistoryEvent(historyEvent, patch, null, entityType, ownerId, key, eventType, dateParts, imageKeys, result, apply, change);
            if (change.Fields.Count > 0 || existing is null)
            {
                change.ChangeType = existing is null ? "Created" : "Updated";
                changed = true;
            }

            if (apply && (change.Fields.Count > 0 || existing is null))
            {
                historyEvent = existing is null ? await processorContext.historyEventRepository.CreateAsync(historyEvent, cancellationToken) : await processorContext.historyEventRepository.UpdateAsync(historyEvent.Id, historyEvent, cancellationToken) ?? historyEvent;
                change.EntityId = historyEvent.Id;
            }

            result.Changes.Add(change);
        }

        return changed;
    }
}
