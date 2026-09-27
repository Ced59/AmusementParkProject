namespace AmusementPark.Core.Domain.History;

public static class HistoricalRelationRevisionValidator
{
    public static void ValidatePredecessor(HistoricalRelation relation, HistoricalRelation? predecessor)
    {
        ArgumentNullException.ThrowIfNull(relation);
        if (relation.Revision == 1)
        {
            if (predecessor is not null)
            {
                throw Invalid();
            }

            return;
        }

        if (predecessor is null
            || predecessor.Id != relation.Id
            || predecessor.Revision != relation.Revision - 1
            || relation.SupersedesRevision != predecessor.Revision
            || predecessor.RevisionOrigin != relation.RevisionOrigin
            || predecessor.RecordedAtUtc > relation.RecordedAtUtc
            || !IsWorkflowTransitionValid(relation, predecessor))
        {
            throw Invalid();
        }
    }

    private static bool IsWorkflowTransitionValid(
        HistoricalRelation relation,
        HistoricalRelation predecessor)
    {
        return relation.WorkflowState switch
        {
            HistoricalEditorialWorkflowState.Corrected => predecessor.WorkflowState
                    is HistoricalEditorialWorkflowState.Published or HistoricalEditorialWorkflowState.Corrected
                && predecessor.PublicationState == HistoricalPublicationState.Published,
            HistoricalEditorialWorkflowState.Retracted => predecessor.WorkflowState
                    is HistoricalEditorialWorkflowState.Published or HistoricalEditorialWorkflowState.Corrected
                && predecessor.PublicationState == HistoricalPublicationState.Published,
            _ => predecessor.WorkflowState < HistoricalEditorialWorkflowState.Published
                && relation.WorkflowState >= predecessor.WorkflowState
                && (int)relation.WorkflowState <= (int)predecessor.WorkflowState + 1,
        };
    }

    private static HistoricalPersistenceValidationException Invalid()
    {
        return new HistoricalPersistenceValidationException(
            HistoricalPersistenceErrorCodes.InvalidRevision,
            "A historical relation correction must supersede its immediately prior revision.");
    }
}
