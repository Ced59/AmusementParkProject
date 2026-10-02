using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalCanonicalResourceRetractionService
{
    private readonly IHistoricalFactRepository factRepository;
    private readonly HistoricalNarrativeCanonicalFactRetractionService factRetractionService;
    private readonly HistoricalCanonicalSourceRetractionService sourceRetractionService;

    public HistoricalCanonicalResourceRetractionService(
        IHistoricalFactRepository factRepository,
        HistoricalNarrativeCanonicalFactRetractionService factRetractionService,
        HistoricalCanonicalSourceRetractionService sourceRetractionService)
    {
        this.factRepository = factRepository
            ?? throw new ArgumentNullException(nameof(factRepository));
        this.factRetractionService = factRetractionService
            ?? throw new ArgumentNullException(nameof(factRetractionService));
        this.sourceRetractionService = sourceRetractionService
            ?? throw new ArgumentNullException(nameof(sourceRetractionService));
    }

    public async Task RetractAsync(Guid factId, CancellationToken cancellationToken)
    {
        HistoricalFact? latest = await this.factRepository.GetLatestRevisionAsync(
            factId,
            cancellationToken);
        if (latest is null)
        {
            throw new InvalidOperationException(
                "A canonical historical fact referenced by a narrative is missing.");
        }

        Guid[] sourceIds = latest.SourceReferences
            .Select(static reference => reference.SourceId)
            .Distinct()
            .ToArray();

        List<HistoricalSourceReference> retractedSourceSnapshots = new List<HistoricalSourceReference>();
        try
        {
            await this.factRetractionService.RetractAsync(factId, cancellationToken);
            foreach (Guid sourceId in sourceIds)
            {
                HistoricalSourceReference? snapshot = await this.sourceRetractionService.RetractAsync(
                    sourceId,
                    cancellationToken);
                if (snapshot is not null)
                {
                    retractedSourceSnapshots.Add(snapshot);
                }
            }
        }
        catch (Exception retractionException)
        {
            List<Exception> compensationExceptions = new List<Exception>();
            foreach (HistoricalSourceReference snapshot in retractedSourceSnapshots.AsEnumerable().Reverse())
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

            try
            {
                await this.factRetractionService.RestoreAsync(latest, CancellationToken.None);
            }
            catch (Exception compensationException)
            {
                compensationExceptions.Add(compensationException);
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
}
