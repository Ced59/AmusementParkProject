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

    public async Task<HistoricalSourceReference?> RetractAsync(
        Guid sourceId,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalSourceReference? latest = await this.sourceRepository.GetLatestRevisionAsync(
                sourceId,
                cancellationToken);
            if (latest is null
                || latest.PublicationState == HistoricalPublicationState.Withdrawn)
            {
                return null;
            }

            HistoricalSourceReference retraction = CreateRevision(
                latest,
                HistoricalSourceAccessibility.Withdrawn,
                HistoricalEditorialWorkflowState.Retracted,
                HistoricalPublicationState.Withdrawn);
            HistoricalReviewEvent reviewEvent = CreateReviewEvent(
                retraction,
                HistoricalReviewEventType.Retracted,
                "Retrait de la source transitoire après remplacement par une ressource HIST canonique.");
            HistoricalRevisionWriteDisposition disposition = await this.sourceRepository
                .AppendRevisionAsync(retraction, reviewEvent, cancellationToken);
            if (disposition is HistoricalRevisionWriteDisposition.Created
                or HistoricalRevisionWriteDisposition.AlreadyExists)
            {
                return latest;
            }
        }

        throw new InvalidOperationException(
            "The historical source changed concurrently and could not be retracted safely.");
    }

    public async Task RestoreAsync(
        HistoricalSourceReference snapshot,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalSourceReference? latest = await this.sourceRepository.GetLatestRevisionAsync(
                snapshot.Id,
                cancellationToken);
            if (latest is null)
            {
                throw new InvalidOperationException(
                    "A historical source could not be restored because its latest revision is missing.");
            }

            if (latest.PublicationState != HistoricalPublicationState.Withdrawn)
            {
                return;
            }

            HistoricalSourceReference restoration = CreateRevision(
                latest,
                snapshot.Accessibility,
                snapshot.WorkflowState,
                snapshot.PublicationState,
                snapshot);
            HistoricalReviewEvent reviewEvent = CreateReviewEvent(
                restoration,
                ResolveRestorationEventType(snapshot.WorkflowState),
                "Restauration compensatoire après l'échec du retrait d'une ressource HIST canonique.");
            HistoricalRevisionWriteDisposition disposition = await this.sourceRepository
                .AppendRevisionAsync(restoration, reviewEvent, cancellationToken);
            if (disposition is HistoricalRevisionWriteDisposition.Created
                or HistoricalRevisionWriteDisposition.AlreadyExists)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "The historical source changed concurrently and could not be restored safely.");
    }

    private static HistoricalSourceReference CreateRevision(
        HistoricalSourceReference latest,
        HistoricalSourceAccessibility accessibility,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalSourceReference? values = null)
    {
        HistoricalSourceReference sourceValues = values ?? latest;
        DateTime nowUtc = DateTime.UtcNow;
        DateTime recordedAtUtc = nowUtc > latest.RecordedAtUtc
            ? nowUtc
            : latest.RecordedAtUtc;
        return new HistoricalSourceReference(
            latest.Id,
            latest.Revision + 1,
            sourceValues.Type,
            sourceValues.Title,
            sourceValues.PublisherOrAuthor,
            sourceValues.Url,
            sourceValues.BibliographicReference,
            sourceValues.PublishedOn,
            sourceValues.AccessedOn,
            sourceValues.LanguageCode,
            sourceValues.ArchiveUrl,
            sourceValues.Scopes,
            sourceValues.AdminNote,
            accessibility,
            workflowState,
            publicationState,
            recordedAtUtc,
            sourceValues.RevisionOrigin);
    }

    private static HistoricalReviewEvent CreateReviewEvent(
        HistoricalSourceReference source,
        HistoricalReviewEventType eventType,
        string note)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            eventType,
            "system:history-canonicalization",
            note,
            source.RecordedAtUtc);
    }

    private static HistoricalReviewEventType ResolveRestorationEventType(
        HistoricalEditorialWorkflowState workflowState)
    {
        return workflowState switch
        {
            HistoricalEditorialWorkflowState.Draft => HistoricalReviewEventType.DraftUpdated,
            HistoricalEditorialWorkflowState.EditorialReview
                or HistoricalEditorialWorkflowState.StructuredValidation =>
                    HistoricalReviewEventType.ReviewUpdated,
            HistoricalEditorialWorkflowState.Published => HistoricalReviewEventType.Published,
            HistoricalEditorialWorkflowState.Corrected => HistoricalReviewEventType.Corrected,
            _ => throw new InvalidOperationException(
                "The historical source snapshot cannot be restored to its previous workflow state."),
        };
    }
}
