using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.ParkGraphUpserts.Results;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphHistoricalNarrativePersistence
{
    internal static async Task<bool> DetectMissingCanonicalFactAsync(
        ParkGraphUpsertProcessor processorContext,
        HistoryEvent? existing,
        HistoryEvent historyEvent,
        bool narrativeChanged,
        ParkGraphUpsertChange change,
        CancellationToken cancellationToken)
    {
        if (existing is null
            || narrativeChanged
            || processorContext.historicalNarrativeCanonicalizer is null)
        {
            return false;
        }

        bool canonicalRepairRequired = await processorContext.historicalNarrativeCanonicalizer
            .NeedsCanonicalRepairAsync(historyEvent, cancellationToken);
        if (!canonicalRepairRequired)
        {
            return false;
        }

        change.ChangeType = "Updated";
        change.Fields.Add(new ParkGraphUpsertFieldChange
        {
            Field = "canonicalHistory",
            OldValue = "incomplete",
            NewValue = "restored",
        });
        return true;
    }

    internal static async Task<HistoryEvent> PersistAsync(
        ParkGraphUpsertProcessor processorContext,
        HistoryEvent? existing,
        HistoryEvent historyEvent,
        bool narrativeChanged,
        bool canonicalRepairRequired,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        string key,
        ParkGraphUpsertResult result,
        CancellationToken cancellationToken)
    {
        if (processorContext.historyEventRepository is null)
        {
            throw new InvalidOperationException(
                "Historical narrative persistence is required for Park Graph updates.");
        }

        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot = null;
        if (existing is not null && (narrativeChanged || canonicalRepairRequired))
        {
            retractionSnapshot = await processorContext.RetractCanonicalResourcesBeforeHistoryMutationAsync(
                existing,
                cancellationToken);
        }

        if (existing is null)
        {
            historyEvent = await processorContext.historyEventRepository.CreateAsync(
                historyEvent,
                cancellationToken);
        }
        else if (narrativeChanged)
        {
            historyEvent = await ParkGraphHistoricalNarrativeUpdater.UpdateAsync(
                processorContext,
                historyEvent,
                expectedUpdatedAtUtc,
                expectedCanonicalFactId,
                retractionSnapshot,
                cancellationToken);
        }

        await processorContext.CanonicalizeHistoryNarrativeAsync(
            historyEvent,
            expectedCanonicalFactId,
            retractionSnapshot,
            key,
            result,
            cancellationToken);
        return historyEvent;
    }
}
