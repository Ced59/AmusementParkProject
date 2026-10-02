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
                if (canonicalization.CanonicalFactId.HasValue)
                {
                    await this.resourceRetractionService.RetractAsync(
                        canonicalization.CanonicalFactId.Value,
                        cancellationToken);
                }

                throw new InvalidOperationException(
                    "A historical narrative changed while its canonical migration was running.");
            }

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
