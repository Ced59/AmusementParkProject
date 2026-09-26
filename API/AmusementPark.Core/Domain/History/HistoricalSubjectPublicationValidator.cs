namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Applique la politique de publication du sujet après résolution de son état courant.
/// </summary>
public static class HistoricalSubjectPublicationValidator
{
    public static void Validate(HistoricalFact fact, bool currentSubjectIsPublic)
    {
        ArgumentNullException.ThrowIfNull(fact);
        bool isPublicRevision = fact.PublicationState is HistoricalPublicationState.Published
            or HistoricalPublicationState.LegacyPublishedPendingReview;
        if (isPublicRevision
            && fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.FollowCurrentSubject
            && !currentSubjectIsPublic)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A public historical fact cannot follow a current subject that is not public.");
        }
    }
}
