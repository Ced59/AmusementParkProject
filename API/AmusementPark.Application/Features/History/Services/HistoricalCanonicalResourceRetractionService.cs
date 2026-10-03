using AmusementPark.Application.Features.History.Models;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalCanonicalResourceRetractionService
{
    private readonly HistoricalNarrativeCanonicalFactRetractionService factRetractionService;
    private readonly HistoricalCanonicalSourceRetractionService sourceRetractionService;

    public HistoricalCanonicalResourceRetractionService(
        HistoricalNarrativeCanonicalFactRetractionService factRetractionService,
        HistoricalCanonicalSourceRetractionService sourceRetractionService)
    {
        this.factRetractionService = factRetractionService
            ?? throw new ArgumentNullException(nameof(factRetractionService));
        this.sourceRetractionService = sourceRetractionService
            ?? throw new ArgumentNullException(nameof(sourceRetractionService));
    }

    public async Task<HistoricalCanonicalResourceRetractionSnapshot> RetractAsync(
        Guid factId,
        CancellationToken cancellationToken)
    {
        HistoricalFact? attemptedFactSnapshot = null;
        Dictionary<Guid, HistoricalSourceReference> attemptedSourceSnapshots =
            new Dictionary<Guid, HistoricalSourceReference>();
        try
        {
            HistoricalFact retractedFactSnapshot = await this.factRetractionService.RetractAsync(
                factId,
                snapshot => attemptedFactSnapshot = snapshot,
                cancellationToken);
            Guid[] sourceIds = retractedFactSnapshot.SourceReferences
                .Select(static reference => reference.SourceId)
                .Distinct()
                .ToArray();
            foreach (Guid sourceId in sourceIds)
            {
                await this.sourceRetractionService.RetractAsync(
                    sourceId,
                    snapshot => RegisterSourceAttempt(
                        attemptedSourceSnapshots,
                        sourceId,
                        snapshot),
                    cancellationToken);
            }

            return new HistoricalCanonicalResourceRetractionSnapshot(
                attemptedFactSnapshot,
                attemptedSourceSnapshots.Values.ToArray());
        }
        catch (Exception retractionException)
        {
            HistoricalCanonicalResourceRetractionSnapshot snapshot =
                new HistoricalCanonicalResourceRetractionSnapshot(
                    attemptedFactSnapshot,
                    attemptedSourceSnapshots.Values.ToArray());
            try
            {
                await this.RestoreAsync(snapshot, CancellationToken.None);
            }
            catch (Exception compensationException)
            {
                throw new AggregateException(
                    "Canonical historical resources could not be retracted or fully restored safely.",
                    retractionException,
                    compensationException);
            }

            throw;
        }
    }

    public async Task RestoreAsync(
        HistoricalCanonicalResourceRetractionSnapshot snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        List<Exception> compensationExceptions = new List<Exception>();
        foreach (HistoricalSourceReference sourceSnapshot in snapshot.Sources.Reverse())
        {
            try
            {
                await this.sourceRetractionService.RestoreAsync(
                    sourceSnapshot,
                    cancellationToken);
            }
            catch (Exception compensationException)
            {
                compensationExceptions.Add(compensationException);
            }
        }

        if (snapshot.Fact is not null)
        {
            try
            {
                await this.factRetractionService.RestoreAsync(
                    snapshot.Fact,
                    cancellationToken);
            }
            catch (Exception compensationException)
            {
                compensationExceptions.Add(compensationException);
            }
        }

        if (compensationExceptions.Count > 0)
        {
            throw new AggregateException(
                "Canonical historical resources could not be fully restored safely.",
                compensationExceptions);
        }
    }

    private static void RegisterSourceAttempt(
        IDictionary<Guid, HistoricalSourceReference> attemptedSourceSnapshots,
        Guid sourceId,
        HistoricalSourceReference? snapshot)
    {
        if (snapshot is null)
        {
            attemptedSourceSnapshots.Remove(sourceId);
            return;
        }

        attemptedSourceSnapshots[sourceId] = snapshot;
    }
}
