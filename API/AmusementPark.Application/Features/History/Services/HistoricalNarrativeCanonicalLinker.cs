using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public static class HistoricalNarrativeCanonicalLinker
{
    public static async Task LinkAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService? resourceRetractionService,
        HistoryEvent historyEvent,
        HistoricalNarrativeCanonicalizationResult canonicalization,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(historyEventRepository);
        ArgumentNullException.ThrowIfNull(historyEvent);
        ArgumentNullException.ThrowIfNull(canonicalization);

        Exception? initialFailure = null;
        bool linked;
        try
        {
            linked = await SetCanonicalizationAsync(
                historyEventRepository,
                historyEvent,
                canonicalization,
                cancellationToken);
        }
        catch (Exception exception)
        {
            initialFailure = exception;
            try
            {
                linked = await SetCanonicalizationAsync(
                    historyEventRepository,
                    historyEvent,
                    canonicalization,
                    CancellationToken.None);
            }
            catch (Exception retryException)
            {
                await ReconcileOrCleanupAsync(
                    historyEventRepository,
                    resourceRetractionService,
                    historyEvent,
                    canonicalization,
                    new AggregateException(
                        "The canonical HIST link could not be confirmed after retry.",
                        exception,
                        retryException));
                return;
            }
        }

        if (linked)
        {
            return;
        }

        InvalidOperationException conflictException = new InvalidOperationException(
            "The historical narrative changed while its canonical HIST fact was being linked.");
        Exception failure = initialFailure is null
            ? conflictException
            : new AggregateException(
                "The canonical HIST link failed and its retry lost a concurrent mutation.",
                initialFailure,
                conflictException);
        await ReconcileOrCleanupAsync(
            historyEventRepository,
            resourceRetractionService,
            historyEvent,
            canonicalization,
            failure);
    }

    private static Task<bool> SetCanonicalizationAsync(
        IHistoryEventRepository historyEventRepository,
        HistoryEvent historyEvent,
        HistoricalNarrativeCanonicalizationResult canonicalization,
        CancellationToken cancellationToken)
    {
        return historyEventRepository.SetCanonicalizationAsync(
            historyEvent.Id,
            historyEvent.UpdatedAtUtc,
            canonicalization.CanonicalFactId,
            canonicalization.State,
            HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
            canonicalization.Warnings,
            cancellationToken);
    }

    private static async Task ReconcileOrCleanupAsync(
        IHistoryEventRepository historyEventRepository,
        HistoricalCanonicalResourceRetractionService? resourceRetractionService,
        HistoryEvent historyEvent,
        HistoricalNarrativeCanonicalizationResult canonicalization,
        Exception failure)
    {
        HistoryEvent? durableNarrative;
        try
        {
            durableNarrative = await historyEventRepository.GetByIdAsync(
                historyEvent.Id,
                true,
                CancellationToken.None);
        }
        catch (Exception reconciliationException)
        {
            throw new AggregateException(
                "The canonical HIST link and its durable state could not be confirmed safely.",
                failure,
                reconciliationException);
        }

        if (durableNarrative is not null
            && durableNarrative.UpdatedAtUtc == historyEvent.UpdatedAtUtc
            && durableNarrative.CanonicalFactId == canonicalization.CanonicalFactId
            && durableNarrative.CanonicalizationState == canonicalization.State)
        {
            return;
        }

        if (canonicalization.CanonicalFactId.HasValue)
        {
            if (resourceRetractionService is null)
            {
                throw new AggregateException(
                    "The failed canonical HIST link requires canonical resource retraction.",
                    failure,
                    new InvalidOperationException(
                        "Canonical resource retraction is not configured."));
            }

            try
            {
                await resourceRetractionService.RetractAsync(
                    canonicalization.CanonicalFactId.Value,
                    CancellationToken.None);
            }
            catch (Exception cleanupException)
            {
                throw new AggregateException(
                    "The failed canonical HIST link left resources that could not be withdrawn safely.",
                    failure,
                    cleanupException);
            }
        }

        throw failure;
    }
}
