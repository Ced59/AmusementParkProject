namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Protège la continuité d'une chaîne de révisions de source immuables.
/// </summary>
public static class HistoricalSourceRevisionValidator
{
    public static void ValidatePredecessor(
        HistoricalSourceReference source,
        HistoricalSourceReference? predecessor)
    {
        ArgumentNullException.ThrowIfNull(source);
        if (source.Revision == 1)
        {
            if (predecessor is not null)
            {
                throw Invalid();
            }

            return;
        }

        if (predecessor is null
            || predecessor.Id != source.Id
            || predecessor.Revision != source.Revision - 1
            || predecessor.RecordedAtUtc > source.RecordedAtUtc
            || !IsWorkflowTransitionValid(source, predecessor))
        {
            throw Invalid();
        }
    }

    private static bool IsWorkflowTransitionValid(
        HistoricalSourceReference source,
        HistoricalSourceReference predecessor)
    {
        return source.WorkflowState switch
        {
            HistoricalEditorialWorkflowState.Corrected =>
                predecessor.WorkflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected
                && predecessor.PublicationState == HistoricalPublicationState.Published,
            HistoricalEditorialWorkflowState.Retracted =>
                predecessor.WorkflowState is HistoricalEditorialWorkflowState.Published
                    or HistoricalEditorialWorkflowState.Corrected
                && predecessor.PublicationState == HistoricalPublicationState.Published,
            _ => predecessor.WorkflowState < HistoricalEditorialWorkflowState.Published
                && source.WorkflowState >= predecessor.WorkflowState
                && source.WorkflowState <= HistoricalEditorialWorkflowState.Published,
        };
    }

    private static HistoricalPersistenceValidationException Invalid()
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidRevision,
            "A historical source correction must follow its existing immediately prior revision.");
    }
}
