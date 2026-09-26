namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Vérifie qu'un événement d'audit décrit réellement la révision immuable qu'il cible.
/// </summary>
public static class HistoricalReviewEventTargetValidator
{
    public static void ValidateFactTarget(HistoricalReviewEvent reviewEvent, HistoricalFact? fact)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        if (reviewEvent.ResourceType != HistoricalReviewResourceType.Fact
            || fact is null
            || fact.Id != reviewEvent.ResourceId
            || fact.Revision != reviewEvent.ResourceRevision
            || reviewEvent.OccurredAtUtc < fact.RecordedAtUtc
            || !MatchesLifecycle(
                reviewEvent.EventType,
                fact.Revision,
                fact.WorkflowState,
                fact.PublicationState,
                fact.RevisionOrigin,
                supportsSourcesAttached: true))
        {
            throw Invalid();
        }
    }

    public static void ValidateSourceTarget(
        HistoricalReviewEvent reviewEvent,
        HistoricalSourceReference? source)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        if (reviewEvent.ResourceType != HistoricalReviewResourceType.Source
            || source is null
            || source.Id != reviewEvent.ResourceId
            || source.Revision != reviewEvent.ResourceRevision
            || reviewEvent.OccurredAtUtc < source.RecordedAtUtc
            || !MatchesLifecycle(
                reviewEvent.EventType,
                source.Revision,
                source.WorkflowState,
                source.PublicationState,
                source.RevisionOrigin,
                supportsSourcesAttached: false))
        {
            throw Invalid();
        }
    }

    public static void ValidateFactTransition(
        HistoricalReviewEvent reviewEvent,
        HistoricalFact fact,
        HistoricalFact? predecessor)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        ArgumentNullException.ThrowIfNull(fact);
        ValidateSameStageReviewEvent(
            reviewEvent.EventType,
            fact.WorkflowState,
            predecessor?.WorkflowState);
    }

    public static void ValidateSourceTransition(
        HistoricalReviewEvent reviewEvent,
        HistoricalSourceReference source,
        HistoricalSourceReference? predecessor)
    {
        ArgumentNullException.ThrowIfNull(reviewEvent);
        ArgumentNullException.ThrowIfNull(source);
        ValidateSameStageReviewEvent(
            reviewEvent.EventType,
            source.WorkflowState,
            predecessor?.WorkflowState);
    }

    private static bool MatchesLifecycle(
        HistoricalReviewEventType eventType,
        int revision,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalPublicationState publicationState,
        HistoricalRevisionOrigin revisionOrigin,
        bool supportsSourcesAttached)
    {
        return eventType switch
        {
            HistoricalReviewEventType.Created => revision == 1
                && revisionOrigin == HistoricalRevisionOrigin.Ordinary
                && workflowState == HistoricalEditorialWorkflowState.Draft
                && publicationState == HistoricalPublicationState.Draft,
            HistoricalReviewEventType.SourcesAttached => supportsSourcesAttached
                && workflowState == HistoricalEditorialWorkflowState.SourcesAttached,
            HistoricalReviewEventType.SubmittedForEditorialReview =>
                workflowState == HistoricalEditorialWorkflowState.EditorialReview
                && (revision != 1 || revisionOrigin != HistoricalRevisionOrigin.LegacyMigration),
            HistoricalReviewEventType.StructuredValidationCompleted =>
                workflowState == HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalReviewEventType.Published =>
                workflowState == HistoricalEditorialWorkflowState.Published
                && publicationState == HistoricalPublicationState.Published,
            HistoricalReviewEventType.Corrected => revision > 1
                && workflowState == HistoricalEditorialWorkflowState.Corrected
                && publicationState == HistoricalPublicationState.Published,
            HistoricalReviewEventType.Retracted => revision > 1
                && workflowState == HistoricalEditorialWorkflowState.Retracted
                && publicationState == HistoricalPublicationState.Withdrawn,
            HistoricalReviewEventType.Migrated => revision == 1
                && revisionOrigin == HistoricalRevisionOrigin.LegacyMigration
                && workflowState == HistoricalEditorialWorkflowState.EditorialReview
                && publicationState is HistoricalPublicationState.LegacyPublishedPendingReview
                    or HistoricalPublicationState.Suppressed,
            HistoricalReviewEventType.DraftUpdated => revision > 1
                && workflowState == HistoricalEditorialWorkflowState.Draft
                && publicationState == HistoricalPublicationState.Draft,
            HistoricalReviewEventType.ReviewUpdated => revision > 1
                && (supportsSourcesAttached
                    && workflowState == HistoricalEditorialWorkflowState.SourcesAttached
                    || workflowState is HistoricalEditorialWorkflowState.EditorialReview
                        or HistoricalEditorialWorkflowState.StructuredValidation),
            _ => false,
        };
    }

    private static void ValidateSameStageReviewEvent(
        HistoricalReviewEventType eventType,
        HistoricalEditorialWorkflowState workflowState,
        HistoricalEditorialWorkflowState? predecessorWorkflowState)
    {
        bool remainsInReviewStage = predecessorWorkflowState == workflowState
            && workflowState is HistoricalEditorialWorkflowState.SourcesAttached
                or HistoricalEditorialWorkflowState.EditorialReview
                or HistoricalEditorialWorkflowState.StructuredValidation;
        if (remainsInReviewStage != (eventType == HistoricalReviewEventType.ReviewUpdated))
        {
            throw Invalid();
        }
    }

    private static HistoricalPersistenceValidationException Invalid()
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidReviewEvent,
            "A historical review event must target an existing compatible resource revision.");
    }
}
