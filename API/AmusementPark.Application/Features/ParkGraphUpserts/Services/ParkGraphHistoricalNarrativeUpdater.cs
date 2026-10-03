using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.ParkGraphUpserts.Services;

internal static class ParkGraphHistoricalNarrativeUpdater
{
    internal static async Task<HistoryEvent> UpdateAsync(
        ParkGraphUpsertProcessor processorContext,
        HistoryEvent historyEvent,
        DateTime expectedUpdatedAtUtc,
        Guid? expectedCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? retractionSnapshot,
        CancellationToken cancellationToken)
    {
        if (processorContext.historyEventRepository is null)
        {
            throw new InvalidOperationException(
                "Historical narrative persistence is required for Park Graph updates.");
        }

        HistoryEvent? updatedHistoryEvent;
        try
        {
            updatedHistoryEvent = await processorContext.historyEventRepository.UpdateAsync(
                historyEvent.Id,
                historyEvent,
                expectedUpdatedAtUtc,
                expectedCanonicalFactId,
                cancellationToken);
        }
        catch (Exception mutationException)
        {
            await processorContext.RestoreCanonicalResourcesAfterHistoryMutationFailureAsync(
                historyEvent.Id,
                expectedUpdatedAtUtc,
                expectedCanonicalFactId,
                retractionSnapshot,
                mutationException);
            throw;
        }

        if (updatedHistoryEvent is not null)
        {
            return updatedHistoryEvent;
        }

        InvalidOperationException conflictException = new InvalidOperationException(
            "The historical narrative changed concurrently and could not be updated safely.");
        await processorContext.RestoreCanonicalResourcesAfterHistoryMutationFailureAsync(
            historyEvent.Id,
            expectedUpdatedAtUtc,
            expectedCanonicalFactId,
            retractionSnapshot,
            conflictException);
        throw conflictException;
    }
}
