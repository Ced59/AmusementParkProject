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

    public async Task<HistoricalFact> RetractAsync(
        Guid factId,
        Action<HistoricalFact?> onRetractionAttempt,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(onRetractionAttempt);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalFact? latest = await this.historicalFactRepository.GetLatestRevisionAsync(
                factId,
                cancellationToken);
            if (latest is null)
            {
                onRetractionAttempt(null);
                throw new InvalidOperationException(
                    "A canonical historical fact referenced by a narrative is missing.");
            }

            if (latest.PublicationState == HistoricalPublicationState.Withdrawn)
            {
                onRetractionAttempt(null);
                return latest;
            }

            onRetractionAttempt(latest);
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
                return latest;
            }
        }

        onRetractionAttempt(null);
        throw new InvalidOperationException(
            "The canonical historical fact changed concurrently and could not be retracted safely.");
    }

    public async Task RestoreAsync(HistoricalFact snapshot, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        for (int attempt = 0; attempt < 2; attempt++)
        {
            HistoricalFact? latest = await this.historicalFactRepository.GetLatestRevisionAsync(
                snapshot.Id,
                cancellationToken);
            if (latest is null)
            {
                throw new InvalidOperationException(
                    "A canonical historical fact could not be restored because its latest revision is missing.");
            }

            bool isMatchingRetraction = latest.PublicationState == HistoricalPublicationState.Withdrawn
                && latest.Revision == snapshot.Revision + 1
                && latest.SupersedesRevision == snapshot.Revision;
            if (!isMatchingRetraction)
            {
                return;
            }

            DateTime nowUtc = DateTime.UtcNow;
            DateTime recordedAtUtc = nowUtc > latest.RecordedAtUtc
                ? nowUtc
                : latest.RecordedAtUtc;
            HistoricalFact restoration = new HistoricalFact(
                snapshot.Id,
                snapshot.Subject,
                snapshot.Type,
                snapshot.Period,
                snapshot.State,
                snapshot.Importance,
                snapshot.WorkflowState,
                snapshot.PublicationState,
                snapshot.PublicUncertaintyExplanation,
                snapshot.LifecycleBoundaryMeaning,
                snapshot.AttributeKind,
                snapshot.AttributeBoundaryMeaning,
                snapshot.SequenceWithinDate,
                snapshot.SourceReferences,
                snapshot.StructuredValue,
                snapshot.OtherTypeLabel,
                snapshot.NarrativeContentId,
                snapshot.VerifiedAtUtc,
                snapshot.PublishedAtUtc,
                snapshot.PublicationMethodologyVersion,
                latest.Revision + 1,
                latest.Revision,
                recordedAtUtc,
                snapshot.RevisionOrigin);
            HistoricalReviewEvent reviewEvent = new HistoricalReviewEvent(
                Guid.NewGuid(),
                HistoricalReviewResourceType.Fact,
                restoration.Id,
                restoration.Revision,
                ResolveRestorationEventType(snapshot.WorkflowState),
                "system:history-narrative-change",
                "Restauration compensatoire après l'échec du retrait d'une ressource HIST canonique.",
                recordedAtUtc);
            HistoricalRevisionWriteDisposition outcome =
                await this.historicalFactRepository.AppendRevisionAsync(
                    restoration,
                    reviewEvent,
                    cancellationToken);
            if (outcome is HistoricalRevisionWriteDisposition.Created
                or HistoricalRevisionWriteDisposition.AlreadyExists)
            {
                return;
            }
        }

        throw new InvalidOperationException(
            "The canonical historical fact changed concurrently and could not be restored safely.");
    }

    private static HistoricalReviewEventType ResolveRestorationEventType(
        HistoricalEditorialWorkflowState workflowState)
    {
        return workflowState switch
        {
            HistoricalEditorialWorkflowState.Draft => HistoricalReviewEventType.DraftUpdated,
            HistoricalEditorialWorkflowState.SourcesAttached => HistoricalReviewEventType.SourcesAttached,
            HistoricalEditorialWorkflowState.EditorialReview =>
                HistoricalReviewEventType.SubmittedForEditorialReview,
            HistoricalEditorialWorkflowState.StructuredValidation =>
                HistoricalReviewEventType.StructuredValidationCompleted,
            HistoricalEditorialWorkflowState.Published => HistoricalReviewEventType.Published,
            HistoricalEditorialWorkflowState.Corrected => HistoricalReviewEventType.Corrected,
            _ => throw new InvalidOperationException(
                "The canonical historical fact snapshot cannot be restored to its previous workflow state."),
        };
    }
}
