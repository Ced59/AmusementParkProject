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

    public async Task RetractAsync(Guid factId, CancellationToken cancellationToken)
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
        }
        catch (Exception retractionException)
        {
            List<Exception> compensationExceptions = new List<Exception>();
            foreach (HistoricalSourceReference snapshot in attemptedSourceSnapshots.Values.Reverse())
            {
                try
                {
                    await this.sourceRetractionService.RestoreAsync(snapshot, CancellationToken.None);
                }
                catch (Exception compensationException)
                {
                    compensationExceptions.Add(compensationException);
                }
            }

            if (attemptedFactSnapshot is not null)
            {
                try
                {
                    await this.factRetractionService.RestoreAsync(
                        attemptedFactSnapshot,
                        CancellationToken.None);
                }
                catch (Exception compensationException)
                {
                    compensationExceptions.Add(compensationException);
                }
            }

            if (compensationExceptions.Count > 0)
            {
                compensationExceptions.Insert(0, retractionException);
                throw new AggregateException(
                    "Canonical historical resources could not be retracted or fully restored safely.",
                    compensationExceptions);
            }

            throw;
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
