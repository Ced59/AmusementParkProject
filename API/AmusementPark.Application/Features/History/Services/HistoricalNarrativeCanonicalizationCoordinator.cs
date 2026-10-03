using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public static class HistoricalNarrativeCanonicalizationCoordinator
{
    public static async Task<HistoricalNarrativeCanonicalizationResult> ExecuteAsync(
        IHistoryEventRepository historyEventRepository,
        IHistoricalNarrativeCanonicalizer canonicalizer,
        HistoricalCanonicalResourceRetractionService resourceRetractionService,
        HistoryEvent historyEvent,
        Guid? previousCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? previousResourceSnapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEventRepository);
        ArgumentNullException.ThrowIfNull(canonicalizer);
        ArgumentNullException.ThrowIfNull(resourceRetractionService);
        ArgumentNullException.ThrowIfNull(historyEvent);

        HistoricalNarrativeCanonicalizationResult canonicalization;
        try
        {
            canonicalization = await canonicalizer.CanonicalizeAsync(
                historyEvent,
                cancellationToken);
        }
        catch (Exception canonicalizationException)
        {
            await RecoverCanonicalizationFailureAsync(
                historyEventRepository,
                resourceRetractionService,
                historyEvent,
                previousCanonicalFactId,
                previousResourceSnapshot,
                canonicalizationException);
            throw;
        }

        await HistoricalNarrativeCanonicalLinker.LinkAsync(
            historyEventRepository,
            resourceRetractionService,
            historyEvent,
            canonicalization,
            cancellationToken,
            failure => RestorePreviousResourcesIfNeededAsync(
                historyEventRepository,
                resourceRetractionService,
                historyEvent,
                previousCanonicalFactId,
                previousResourceSnapshot,
                failure));
        return canonicalization;
    }

    private static async Task RecoverCanonicalizationFailureAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService resourceRetractionService,
        HistoryEvent historyEvent,
        Guid? previousCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? previousResourceSnapshot,
        Exception canonicalizationException)
    {
        List<Exception> recoveryExceptions = new List<Exception>();
        Guid generatedFactId = HistoricalNarrativeCanonicalIdentity.CreateGuid(
            HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
            "fact",
            historyEvent.Id,
            historyEvent.UpdatedAtUtc);
        Guid[] generatedSourceIds = historyEvent.Sources
            .Select((_, index) => HistoricalNarrativeCanonicalIdentity.CreateGuid(
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                "source",
                historyEvent.Id,
                historyEvent.UpdatedAtUtc,
                index))
            .ToArray();
        try
        {
            await resourceRetractionService.RetractGeneratedAsync(
                generatedFactId,
                generatedSourceIds,
                CancellationToken.None);
        }
        catch (Exception cleanupException)
        {
            recoveryExceptions.Add(cleanupException);
        }

        try
        {
            await RestorePreviousResourcesIfNeededAsync(
                historyEventRepository,
                resourceRetractionService,
                historyEvent,
                previousCanonicalFactId,
                previousResourceSnapshot,
                canonicalizationException);
        }
        catch (Exception restorationException)
        {
            recoveryExceptions.Add(restorationException);
        }

        if (recoveryExceptions.Count > 0)
        {
            throw new AggregateException(
                "The failed canonical HIST update could not be fully compensated.",
                new[] { canonicalizationException }.Concat(recoveryExceptions));
        }
    }

    private static Task RestorePreviousResourcesIfNeededAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService resourceRetractionService,
        HistoryEvent historyEvent,
        Guid? previousCanonicalFactId,
        HistoricalCanonicalResourceRetractionSnapshot? previousResourceSnapshot,
        Exception failure)
    {
        if (!previousCanonicalFactId.HasValue || previousResourceSnapshot is null)
        {
            return Task.CompletedTask;
        }

        return HistoricalNarrativeMutationRecovery.RestoreIfNarrativeIsUnchangedAsync(
            historyEventRepository,
            resourceRetractionService,
            historyEvent.Id,
            historyEvent.UpdatedAtUtc,
            previousCanonicalFactId.Value,
            previousResourceSnapshot,
            failure);
    }
}
