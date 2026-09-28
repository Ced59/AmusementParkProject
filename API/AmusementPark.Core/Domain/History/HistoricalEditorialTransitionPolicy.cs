namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Décrit les passages autorisés entre les étapes visibles de la revue historique.
/// </summary>
public static class HistoricalEditorialTransitionPolicy
{
    public static HistoricalEditorialWorkflowState GetNextStage(
        HistoricalReviewResourceType resourceType,
        HistoricalEditorialWorkflowState currentStage)
    {
        if (!Enum.IsDefined(resourceType) || !Enum.IsDefined(currentStage))
        {
            throw Invalid("The historical editorial resource or workflow stage is invalid.");
        }

        return (resourceType, currentStage) switch
        {
            (HistoricalReviewResourceType.Fact, HistoricalEditorialWorkflowState.Draft) =>
                HistoricalEditorialWorkflowState.SourcesAttached,
            (HistoricalReviewResourceType.Relation, HistoricalEditorialWorkflowState.Draft) =>
                HistoricalEditorialWorkflowState.SourcesAttached,
            (HistoricalReviewResourceType.Source, HistoricalEditorialWorkflowState.Draft) =>
                HistoricalEditorialWorkflowState.EditorialReview,
            (_, HistoricalEditorialWorkflowState.SourcesAttached) =>
                HistoricalEditorialWorkflowState.EditorialReview,
            (_, HistoricalEditorialWorkflowState.EditorialReview) =>
                HistoricalEditorialWorkflowState.StructuredValidation,
            (_, HistoricalEditorialWorkflowState.StructuredValidation) =>
                HistoricalEditorialWorkflowState.Published,
            _ => throw Invalid("The historical editorial resource cannot advance from its current stage."),
        };
    }

    public static HistoricalReviewEventType GetAdvanceEventType(
        HistoricalEditorialWorkflowState targetStage)
    {
        return targetStage switch
        {
            HistoricalEditorialWorkflowState.SourcesAttached => HistoricalReviewEventType.SourcesAttached,
            HistoricalEditorialWorkflowState.EditorialReview =>
                HistoricalReviewEventType.SubmittedForEditorialReview,
            HistoricalEditorialWorkflowState.StructuredValidation =>
                HistoricalReviewEventType.StructuredValidationCompleted,
            HistoricalEditorialWorkflowState.Published => HistoricalReviewEventType.Published,
            _ => throw Invalid("The target historical editorial stage is not an advance stage."),
        };
    }

    public static HistoricalReviewEventType GetSaveEventType(
        HistoricalEditorialWorkflowState currentStage)
    {
        return currentStage switch
        {
            HistoricalEditorialWorkflowState.Draft => HistoricalReviewEventType.DraftUpdated,
            HistoricalEditorialWorkflowState.SourcesAttached
                or HistoricalEditorialWorkflowState.EditorialReview
                or HistoricalEditorialWorkflowState.StructuredValidation =>
                HistoricalReviewEventType.ReviewUpdated,
            HistoricalEditorialWorkflowState.Published
                or HistoricalEditorialWorkflowState.Corrected => HistoricalReviewEventType.Corrected,
            _ => throw Invalid("The historical editorial resource cannot be edited in its current stage."),
        };
    }

    private static HistoricalPersistenceValidationException Invalid(string message)
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidReviewEvent,
            message);
    }
}
