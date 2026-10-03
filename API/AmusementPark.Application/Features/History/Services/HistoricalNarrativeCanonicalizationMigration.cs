using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalizationMigration
{
    private readonly IHistoryEventRepository historyEventRepository;
    private readonly HistoricalNarrativeCanonicalizationService canonicalizationService;
    private readonly HistoricalCanonicalResourceRetractionService resourceRetractionService;

    public HistoricalNarrativeCanonicalizationMigration(
        IHistoryEventRepository historyEventRepository,
        HistoricalNarrativeCanonicalizationService canonicalizationService,
        HistoricalCanonicalResourceRetractionService resourceRetractionService)
    {
        this.historyEventRepository = historyEventRepository
            ?? throw new ArgumentNullException(nameof(historyEventRepository));
        this.canonicalizationService = canonicalizationService
            ?? throw new ArgumentNullException(nameof(canonicalizationService));
        this.resourceRetractionService = resourceRetractionService
            ?? throw new ArgumentNullException(nameof(resourceRetractionService));
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
            HistoricalNarrativeCanonicalizationResult canonicalization =
                await this.canonicalizationService.MigrateExistingAsync(
                    historyEvent,
                    cancellationToken);
            await HistoricalNarrativeCanonicalLinker.LinkAsync(
                this.historyEventRepository,
                this.resourceRetractionService,
                historyEvent,
                canonicalization,
                cancellationToken);

            if (!previousFactId.HasValue
                || previousFactId == canonicalization.CanonicalFactId)
            {
                continue;
            }

            await this.resourceRetractionService.RetractAsync(
                previousFactId.Value,
                cancellationToken);
        }
    }
}
