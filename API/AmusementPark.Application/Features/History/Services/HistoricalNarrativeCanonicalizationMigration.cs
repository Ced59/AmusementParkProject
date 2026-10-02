using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationMigration
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly IHistoricalFactRepository factRepository;
    private readonly HistoricalNarrativeCanonicalizationService canonicalizationService;
    private readonly HistoricalNarrativeCanonicalFactRetractionService factRetractionService;
    private readonly HistoricalCanonicalSourceRetractionService sourceRetractionService;

    public HistoricalNarrativeCanonicalizationMigration(
        IHistoryEventRepository historyEventRepository,
        IHistoricalFactRepository factRepository,
        HistoricalNarrativeCanonicalizationService canonicalizationService,
        HistoricalNarrativeCanonicalFactRetractionService factRetractionService,
        HistoricalCanonicalSourceRetractionService sourceRetractionService)
    {
        this.historyEventRepository = historyEventRepository
            ?? throw new ArgumentNullException(nameof(historyEventRepository));
        this.factRepository = factRepository ?? throw new ArgumentNullException(nameof(factRepository));
        this.canonicalizationService = canonicalizationService
            ?? throw new ArgumentNullException(nameof(canonicalizationService));
        this.factRetractionService = factRetractionService
            ?? throw new ArgumentNullException(nameof(factRetractionService));
        this.sourceRetractionService = sourceRetractionService
            ?? throw new ArgumentNullException(nameof(sourceRetractionService));
    }

    public async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        IReadOnlyCollection<HistoryEvent> candidates = await this.historyEventRepository
            .GetCanonicalizationCandidatesAsync(
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                cancellationToken);
        foreach (HistoryEvent historyEvent in candidates)
        {
            Guid? previousFactId = historyEvent.CanonicalFactId;
            HistoricalFact? previousFact = previousFactId.HasValue
                ? await this.factRepository.GetLatestRevisionAsync(
                    previousFactId.Value,
                    cancellationToken)
                : null;
            Guid[] previousSourceIds = previousFact?.SourceReferences
                .Select(static reference => reference.SourceId)
                .Distinct()
                .ToArray() ?? Array.Empty<Guid>();
            HistoricalNarrativeCanonicalizationResult canonicalization =
                await this.canonicalizationService.MigrateExistingAsync(
                    historyEvent,
                    cancellationToken);
            bool linked = await this.historyEventRepository.SetCanonicalizationAsync(
                historyEvent.Id,
                historyEvent.UpdatedAtUtc,
                canonicalization.CanonicalFactId,
                canonicalization.State,
                HistoricalNarrativeCanonicalizationService.CanonicalizationVersion,
                canonicalization.Warnings,
                cancellationToken);
            if (!linked)
            {
                throw new InvalidOperationException(
                    "A historical narrative changed while its canonical migration was running.");
            }

            if (!previousFactId.HasValue
                || previousFactId == canonicalization.CanonicalFactId)
            {
                continue;
            }

            await this.factRetractionService.RetractAsync(
                previousFactId.Value,
                cancellationToken);
            foreach (Guid sourceId in previousSourceIds)
            {
                await this.sourceRetractionService.RetractAsync(sourceId, cancellationToken);
            }
        }
    }
}
