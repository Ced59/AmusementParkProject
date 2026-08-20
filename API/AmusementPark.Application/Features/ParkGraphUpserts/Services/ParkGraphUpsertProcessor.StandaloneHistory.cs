using System.Text.Json;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

public sealed partial class ParkGraphUpsertProcessor
{
    private async Task ProcessStandaloneHistoryEventsAsync(
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
            return;
        }

        if (this.historyEventRepository is null)
        {
            result.Warnings.Add("La section history est ignoree car le repository d'historique n'est pas disponible.");
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

            HistoryDateParts? dateParts = ReadHistoryDate(patch);
            if (dateParts is null)
            {
                result.Errors.Add($"L'evenement history '{key ?? eventType}' doit definir une date valide.");
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

            // PatchHistoryEvent only reads the park context for ParkItem events.
            // A standalone event deliberately has no park parent, so the placeholder is never dereferenced.
            PatchHistoryEvent(
                historyEvent,
                patch,
                new Park(),
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
    }
}
