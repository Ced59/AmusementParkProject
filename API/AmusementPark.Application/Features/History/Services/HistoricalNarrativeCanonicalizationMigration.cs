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
            HistoricalCanonicalResourceRetractionSnapshot? previousResourceSnapshot = null;
            try
            {
                HistoricalNarrativeCanonicalizationResult canonicalization =
                    await this.canonicalizationService.MigrateExistingAsync(
                        historyEvent,
                        cancellationToken);
                if (previousFactId.HasValue
                    && previousFactId != canonicalization.CanonicalFactId)
                {
                    previousResourceSnapshot = await this.resourceRetractionService.RetractAsync(
                        previousFactId.Value,
                        cancellationToken);
                }

                await HistoricalNarrativeCanonicalLinker.LinkAsync(
                    this.historyEventRepository,
                    this.resourceRetractionService,
                    historyEvent,
                    canonicalization,
                    cancellationToken);
            }
            catch (Exception migrationException)
            {
                List<Exception> recoveryExceptions = new List<Exception>();
                if (previousFactId != generatedFactId)
                {
                    try
                    {
                        await this.resourceRetractionService.RetractGeneratedAsync(
                            generatedFactId,
                            generatedSourceIds,
                            CancellationToken.None);
                    }
                    catch (Exception cleanupException)
                    {
                        recoveryExceptions.Add(cleanupException);
                    }
                }

                if (previousResourceSnapshot is not null)
                {
                    try
                    {
                        await this.resourceRetractionService.RestoreAsync(
                            previousResourceSnapshot,
                            CancellationToken.None);
                    }
                    catch (Exception restorationException)
                    {
                        recoveryExceptions.Add(restorationException);
                    }
                }

                if (recoveryExceptions.Count > 0)
                {
                    throw new AggregateException(
                        "The canonical narrative migration failed and could not be fully compensated.",
                        new[] { migrationException }.Concat(recoveryExceptions));
                }

                throw;
            }
        }
    }
}
