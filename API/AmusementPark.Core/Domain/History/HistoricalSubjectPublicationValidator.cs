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
        bool requiresDurableParkScope = fact.Subject.PublicationPolicy
                == HistoricalSubjectPublicationPolicy.HistoricalOnly
            && fact.Subject.Type is HistoricalSubjectType.ParkItem or HistoricalSubjectType.ParkZone;
        if (isPublicRevision
            && requiresDurableParkScope
            && string.IsNullOrWhiteSpace(fact.Subject.ContextParkId))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A public historical-only park subject requires a durable park scope.");
        }

        if (isPublicRevision
            && fact.Subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.FollowCurrentSubject
            && !currentSubjectIsPublic)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A public historical fact cannot follow a current subject that is not public.");
        }
    }

    public static void Validate(
        HistoricalRelation relation,
        bool currentSourceIsPublic,
        bool currentTargetIsPublic)
    {
        ArgumentNullException.ThrowIfNull(relation);
        bool isPublicRevision = relation.PublicationState == HistoricalPublicationState.Published;
        ValidateRelationSubject(relation.Source, isPublicRevision, currentSourceIsPublic);
        ValidateRelationSubject(relation.Target, isPublicRevision, currentTargetIsPublic);
    }

    private static void ValidateRelationSubject(
        HistoricalSubject subject,
        bool isPublicRevision,
        bool currentSubjectIsPublic)
    {
        bool requiresDurableParkScope = subject.PublicationPolicy
                == HistoricalSubjectPublicationPolicy.HistoricalOnly
            && subject.Type is HistoricalSubjectType.ParkItem or HistoricalSubjectType.ParkZone;
        if (isPublicRevision && requiresDurableParkScope && string.IsNullOrWhiteSpace(subject.ContextParkId))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidRelation,
                "A public historical-only relation subject requires a durable park scope.");
        }

        if (isPublicRevision
            && (subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.Suppressed
                || !currentSubjectIsPublic))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidRelation,
                "A public relation cannot expose a suppressed subject or a subject outside a public context.");
        }
    }
}
