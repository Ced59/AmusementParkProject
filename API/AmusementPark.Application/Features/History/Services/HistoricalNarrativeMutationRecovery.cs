using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public static class HistoricalNarrativeMutationRecovery
{
    public static async Task<HistoryEvent?> ResolveCommittedUpdateAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService? resourceRetractionService,
        string historyEventId,
        Guid mutationId,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot,
        Exception mutationFailure)
    {
        ArgumentNullException.ThrowIfNull(historyEventRepository);
        try
        {
            HistoryEvent? committedUpdate = await historyEventRepository.GetCommittedUpdateAsync(
                historyEventId,
                mutationId,
                CancellationToken.None);
            if (committedUpdate is not null)
            {
                return committedUpdate;
            }
        }
        catch (Exception reconciliationException)
        {
            throw new AggregateException(
                "The failed historical narrative update could not be reconciled by mutation identifier.",
                mutationFailure,
                reconciliationException);
        }

        if (!expectedCanonicalFactId.HasValue || retractionSnapshot is null)
        {
            return null;
        }

        await RestoreIfNarrativeIsUnchangedAsync(
            historyEventRepository,
            resourceRetractionService
                ?? throw new InvalidOperationException(
                    "Canonical resource retraction is required to restore a failed narrative update."),
            historyEventId,
            expectedUpdatedAtUtc,
            expectedCanonicalFactId.Value,
            retractionSnapshot,
            mutationFailure);
        return null;
    }

    public static async Task RestoreIfNarrativeIsUnchangedAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService resourceRetractionService,
        string historyEventId,
        DateTime expectedUpdatedAtUtc,
        Guid expectedCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot retractionSnapshot,
        Exception mutationFailure)
    {
        ArgumentNullException.ThrowIfNull(historyEventRepository);
        ArgumentNullException.ThrowIfNull(resourceRetractionService);
        ArgumentNullException.ThrowIfNull(retractionSnapshot);
        ArgumentNullException.ThrowIfNull(mutationFailure);

        HistoryEvent? durableNarrative;
        try
        {
            durableNarrative = await historyEventRepository.GetByIdAsync(
                historyEventId,
                true,
                CancellationToken.None);
        }
        catch (Exception reconciliationException)
        {
            throw new AggregateException(
                "The historical narrative mutation failed and its durable state could not be reconciled.",
                mutationFailure,
                reconciliationException);
        }

        bool narrativeIsUnchanged = durableNarrative is not null
            && durableNarrative.UpdatedAtUtc == expectedUpdatedAtUtc
            && durableNarrative.CanonicalFactId == expectedCanonicalFactId;
        if (!narrativeIsUnchanged)
        {
            return;
        }

        try
        {
            await resourceRetractionService.RestoreAsync(
                retractionSnapshot,
                CancellationToken.None);
        }
        catch (Exception restorationException)
        {
            throw new AggregateException(
                "The historical narrative mutation failed and its canonical resources could not be restored.",
                mutationFailure,
                restorationException);
        }
    }
}
