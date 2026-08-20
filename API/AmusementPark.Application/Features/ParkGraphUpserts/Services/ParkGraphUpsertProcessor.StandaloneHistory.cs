using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task PreflightStandaloneHistoryAsync(
        JsonElement root,
        bool createIfMissing,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        if (ResolveHistoryEvents(root) is null)
        {
            return;
        }

        if (this.standaloneAttractionRepository is null)
        {
            result.Errors.Add("Le repository des attractions autonomes n'est pas configure.");
            return;
        }

        if (this.historyEventRepository is null)
        {
            result.Errors.Add("La section history ne peut pas etre appliquee car le repository d'historique n'est pas disponible.");
            return;
        }

        JsonElement? patch = GetObject(root, "standaloneAttraction");
        JsonElement? identity = GetObject(root, "identity");
        JsonElement? migration = GetObject(root, "migration") ?? GetObject(root, "standaloneAttractionMigration");
        StandaloneAttraction? attraction = await this.ResolveStandaloneAttractionAsync(
            patch,
            identity,
            migration,
            createIfMissing,
            this.standaloneAttractionRepository,
            result,
            cancellationToken);
        if (attraction is null)
        {
            return;
        }

        ValidateStandaloneHistoryEvents(root, attraction.Id, result);
    }

    private static void ValidateStandaloneHistoryEvents(
        JsonElement root,
        string? targetStandaloneAttractionId,
        ParkGraphUpsertResult result)
    {
        JsonElement? events = ResolveHistoryEvents(root);
        if (events is null)
        {
            return;
        }

        string? resolvedTargetId = NormalizeString(targetStandaloneAttractionId);
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

            string? key = NormalizeString(ReadString(patch, "key"));
            string? eventType = NormalizeString(ReadString(patch, "eventType") ?? ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                result.Errors.Add("Un evenement history doit definir eventType.");
                continue;
            }

            HistoryEntityType entityType = ResolveHistoryEntityType(patch);
            if (entityType != HistoryEntityType.StandaloneAttraction)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' d'un standaloneAttractionGraph doit cibler 'StandaloneAttraction'.");
                continue;
            }

            string ownerId = NormalizeString(ReadString(patch, "ownerId") ?? ReadString(patch, "standaloneAttractionId"))
                ?? resolvedTargetId;
            if (!string.Equals(ownerId, resolvedTargetId, StringComparison.Ordinal))
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' cible l'attraction autonome '{ownerId}' au lieu de la cible '{resolvedTargetId}'.");
                continue;
            }

            if (!IsValidHistoryEventType(entityType, eventType))
            {
                result.Errors.Add($"Le type d'evenement history '{eventType}' n'est pas valide pour '{entityType}'.");
                continue;
            }

            if (ReadHistoryDate(patch) is null)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' doit definir une date valide.");
            }
        }
    }

    private async Task<bool> ProcessStandaloneHistoryEventsAsync(
        JsonElement root,
        string? targetStandaloneAttractionId,
        Dictionary<string, string> imageKeys,
        ParkGraphUpsertResult result,
        bool apply,
        CancellationToken cancellationToken)
    {
        JsonElement? events = ResolveHistoryEvents(root);
        if (events is null)
        {
            return false;
        }

        if (this.historyEventRepository is null)
        {
            result.Errors.Add("La section history ne peut pas etre traitee car le repository d'historique n'est pas disponible.");
            return false;
        }

        string? resolvedTargetId = NormalizeString(targetStandaloneAttractionId);
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

            string? key = NormalizeString(ReadString(patch, "key"));
            string? eventType = NormalizeString(ReadString(patch, "eventType") ?? ReadString(patch, "type"));
            if (string.IsNullOrWhiteSpace(eventType))
            {
                continue;
            }

            HistoryEntityType entityType = ResolveHistoryEntityType(patch);
            if (entityType != HistoryEntityType.StandaloneAttraction)
            {
                continue;
            }

            string ownerId = NormalizeString(ReadString(patch, "ownerId") ?? ReadString(patch, "standaloneAttractionId"))
                ?? resolvedTargetId;
            if (!string.Equals(ownerId, resolvedTargetId, StringComparison.Ordinal))
            {
                continue;
            }

            if (!IsValidHistoryEventType(entityType, eventType))
            {
                continue;
            }

            HistoryDateParts? dateParts = ReadHistoryDate(patch);
            if (dateParts is null)
            {
                continue;
            }

            key ??= BuildHistoryKey(entityType, ownerId, eventType, dateParts);
            HistoryEvent? existing = await this.historyEventRepository.GetByOwnerKeyAsync(
                entityType,
                ownerId,
                key,
                cancellationToken);
            HistoryEvent historyEvent = existing ?? new HistoryEvent();
            ParkGraphUpsertChange change = BuildEntityChange(
                "HistoryEvent",
                historyEvent.Id,
                key,
                ResolveHistoryDisplayName(patch, eventType),
                existing is null ? "Created" : "Unchanged",
                existing is null ? "key" : "ownerKey");

            PatchHistoryEvent(
                historyEvent,
                patch,
                null,
                entityType,
                ownerId,
                key,
                eventType,
                dateParts,
                imageKeys,
                result,
                apply,
                change);

            if (change.Fields.Count > 0 || existing is null)
            {
                change.ChangeType = existing is null ? "Created" : "Updated";
                changed = true;
            }

            if (apply && (change.Fields.Count > 0 || existing is null))
            {
                historyEvent = existing is null
                    ? await this.historyEventRepository.CreateAsync(historyEvent, cancellationToken)
                    : await this.historyEventRepository.UpdateAsync(historyEvent.Id, historyEvent, cancellationToken) ?? historyEvent;
                change.EntityId = historyEvent.Id;
            }

            result.Changes.Add(change);
        }

        return changed;
    }
}
