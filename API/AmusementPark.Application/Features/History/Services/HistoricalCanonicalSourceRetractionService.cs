using AmusementPark.Application.Features.History.Models;
using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public sealed class HistoricalCanonicalSourceRetractionService
{
    private readonly IHistoricalSourceRepository sourceRepository;

    public HistoricalCanonicalSourceRetractionService(
        IHistoricalSourceRepository sourceRepository)
    {
        this.sourceRepository = sourceRepository
            ?? throw new ArgumentNullException(nameof(sourceRepository));
    }

    public async Task RetractAsync(Guid sourceId, CancellationToken cancellationToken)
    {
        HistoricalSourceReference? latest = await this.sourceRepository.GetLatestRevisionAsync(
            sourceId,
            cancellationToken);
        if (latest is null
            || latest.PublicationState == HistoricalPublicationState.Withdrawn)
        {
            return;
        }

        DateTime nowUtc = DateTime.UtcNow;
        DateTime recordedAtUtc = nowUtc > latest.RecordedAtUtc
            ? nowUtc
            : latest.RecordedAtUtc;
        HistoricalSourceReference retraction = new HistoricalSourceReference(
            latest.Id,
            latest.Revision + 1,
            latest.Type,
            latest.Title,
            latest.PublisherOrAuthor,
            latest.Url,
            latest.BibliographicReference,
            latest.PublishedOn,
            latest.AccessedOn,
            latest.LanguageCode,
            latest.ArchiveUrl,
            latest.Scopes,
            latest.AdminNote,
            HistoricalSourceAccessibility.Withdrawn,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            recordedAtUtc,
            latest.RevisionOrigin);
        HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Source,
            latest.Id,
            retraction.Revision,
            HistoricalReviewEventType.Retracted,
            "system:history-canonicalization",
            "Retrait de la source transitoire après remplacement par une ressource HIST canonique.",
            recordedAtUtc);
        HistoricalRevisionWriteDisposition disposition = await this.sourceRepository
            .AppendRevisionAsync(retraction, reviewEvent, cancellationToken);
        if (disposition == HistoricalRevisionWriteDisposition.Conflict)
        {
            throw new InvalidOperationException(
                "The historical source changed concurrently and could not be retracted safely.");
        }
    }
}
