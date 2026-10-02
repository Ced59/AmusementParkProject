using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalNarrativeCanonicalFactRetractionService
{
    private readonly IHistoricalFactRepository historicalFactRepository;

    public HistoricalNarrativeCanonicalFactRetractionService(
        IHistoricalFactRepository historicalFactRepository)
    {
        this.historicalFactRepository = historicalFactRepository
            ?? throw new ArgumentNullException(nameof(historicalFactRepository));
    }

    public async Task RetractAsync(Guid factId, CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalFact? latest = await this.historicalFactRepository.GetLatestRevisionAsync(
                factId,
                cancellationToken);
            if (latest is null)
            {
                throw new InvalidOperationException(
                    "A canonical historical fact referenced by a narrative is missing.");
            }

            if (latest.PublicationState == HistoricalPublicationState.Withdrawn)
            {
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;
            DateTime recordedAtUtc = nowUtc > latest.RecordedAtUtc
                ? nowUtc
                : latest.RecordedAtUtc;
            HistoricalFact retraction = latest.CreateRetraction(recordedAtUtc);
            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Fact,
                factId,
                retraction.Revision,
                HistoricalReviewEventType.Retracted,
                "system:history-narrative-change",
                "Retrait du fait canonique avant modification ou suppression de son récit administratif.",
                recordedAtUtc);
            HistoricalRevisionWriteDisposition outcome =
                await this.historicalFactRepository.AppendRevisionAsync(
                    retraction,
                    reviewEvent,
                    cancellationToken);
            if (outcome is HistoricalRevisionWriteDisposition.Created
                or HistoricalRevisionWriteDisposition.AlreadyExists)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "The canonical historical fact changed concurrently and could not be retracted safely.");
    }
}
