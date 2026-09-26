namespace AmusementPark.Core.Domain.History;

/// <summary>
/// Vérifie qu'un fait prêt à être utilisé repose sur les révisions de preuve
/// exactes et sur une couverture éditoriale suffisante.
/// </summary>
public static class HistoricalFactEvidenceValidator
{
    public static void Validate(
        HistoricalFact fact,
        IReadOnlyCollection<HistoricalSourceReference> resolvedSources)
    {
        ArgumentNullException.ThrowIfNull(fact);
        ArgumentNullException.ThrowIfNull(resolvedSources);
        if (resolvedSources.Any(static source => source is null))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.MissingSource,
                "A historical fact contains an unresolved source revision.");
        }

        HistoricalSourceReference[] sources = resolvedSources.ToArray();
        bool exactReferencesMatch = sources.Length == fact.SourceReferences.Count
            && sources.Select(static source => (source.Id, source.Revision)).Distinct().Count() == sources.Length
            && fact.SourceReferences.All(reference => sources.Any(source =>
                source.Id == reference.SourceId
                && source.Revision == reference.Revision));
        if (!exactReferencesMatch)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.MissingSource,
                "Every historical fact source must resolve to the exact immutable revision cited.");
        }

        bool everyReferenceIsBoundToFact = fact.SourceReferences.All(reference =>
            reference.SubjectType == fact.Subject.Type
            && string.Equals(reference.SubjectId, fact.Subject.Id, StringComparison.Ordinal)
            && reference.FactType == fact.Type
            && reference.Period == fact.Period
            && (!reference.Scopes.Contains(HistoricalSourceScope.HistoricalLabel)
                || string.Equals(
                    reference.HistoricalLabel,
                    fact.Subject.HistoricalLabel,
                    StringComparison.Ordinal))
            && (!reference.Scopes.Contains(HistoricalSourceScope.StructuredValue)
                || string.Equals(reference.StructuredValue, fact.StructuredValue, StringComparison.Ordinal))
            && (!reference.Scopes.Contains(HistoricalSourceScope.SequenceWithinDate)
                || reference.SequenceWithinDate == fact.SequenceWithinDate)
            && (!reference.Scopes.Contains(HistoricalSourceScope.Narrative)
                || string.Equals(
                    reference.NarrativeContentId,
                    fact.NarrativeContentId,
                    StringComparison.Ordinal))
            && (!reference.Scopes.Contains(HistoricalSourceScope.Period)
                || (reference.LifecycleBoundaryMeaning == fact.LifecycleBoundaryMeaning
                    && reference.AttributeKind == fact.AttributeKind
                    && reference.AttributeBoundaryMeaning == fact.AttributeBoundaryMeaning)));
        if (!everyReferenceIsBoundToFact)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "Every historical source citation must be bound to the fact assertion it supports.");
        }

        bool everyCitationScopeIsDeclaredBySource = fact.SourceReferences.All(reference =>
        {
            HistoricalSourceReference source = sources.Single(candidate =>
                candidate.Id == reference.SourceId
                && candidate.Revision == reference.Revision);
            return reference.Scopes.All(scope => source.Scopes.Contains(scope));
        });
        if (!everyCitationScopeIsDeclaredBySource)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A historical source citation cannot claim a scope absent from its source revision.");
        }

        bool requiresAdmissibleEvidence = fact.State == HistoricalFactState.Verified
            || fact.PublicationState == HistoricalPublicationState.Published;
        if (!requiresAdmissibleEvidence)
        {
            return;
        }

        HashSet<(Guid Id, int Revision)> admissibleSourceKeys = sources
            .Where(static source => source.PublicationState == HistoricalPublicationState.Published
            && (source.WorkflowState is HistoricalEditorialWorkflowState.Published
                or HistoricalEditorialWorkflowState.Corrected)
            && (source.Accessibility is HistoricalSourceAccessibility.Accessible
                or HistoricalSourceAccessibility.Archived))
            .Select(static source => (source.Id, source.Revision))
            .ToHashSet();
        if (admissibleSourceKeys.Count == 0)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A verified or published historical fact requires at least one published and accessible evidence revision.");
        }

        HistoricalSourceRevisionReference[] admissibleReferences = fact.SourceReferences
            .Where(reference => admissibleSourceKeys.Contains((reference.SourceId, reference.Revision)))
            .ToArray();
        ValidateEvidencePositions(fact, admissibleReferences);
        HistoricalSourceRevisionReference[] supportingReferences = admissibleReferences
            .Where(static reference => reference.Position == HistoricalEvidencePosition.Supports)
            .ToArray();

        HistoricalSourceScope[] coreScopes =
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        bool oneSourceCoversCoreAssertion = supportingReferences.Any(reference =>
            coreScopes.All(scope => reference.Scopes.Contains(scope)));
        if (!oneSourceCoversCoreAssertion)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "At least one historical source must cover the subject, fact type, and period together.");
        }

        HashSet<HistoricalSourceScope> coveredScopes = supportingReferences
            .SelectMany(static reference => reference.Scopes)
            .ToHashSet();
        HistoricalSourceScope[] requiredScopes = BuildRequiredScopes(fact);
        if (requiredScopes.Any(scope => !coveredScopes.Contains(scope)))
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "Historical evidence does not cover every structured part of the fact.");
        }
    }

    private static HistoricalSourceScope[] BuildRequiredScopes(HistoricalFact fact)
    {
        List<HistoricalSourceScope> scopes = new List<HistoricalSourceScope>
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        if (fact.StructuredValue is not null)
        {
            scopes.Add(HistoricalSourceScope.StructuredValue);
        }

        if (fact.SequenceWithinDate.HasValue)
        {
            scopes.Add(HistoricalSourceScope.SequenceWithinDate);
        }

        if (fact.NarrativeContentId is not null)
        {
            scopes.Add(HistoricalSourceScope.Narrative);
        }

        return scopes.ToArray();
    }

    private static void ValidateEvidencePositions(
        HistoricalFact fact,
        IReadOnlyCollection<HistoricalSourceRevisionReference> admissibleReferences)
    {
        HistoricalSourceRevisionReference[] supportingReferences = admissibleReferences
            .Where(static reference => reference.Position == HistoricalEvidencePosition.Supports)
            .ToArray();
        HistoricalSourceRevisionReference[] contradictingReferences = admissibleReferences
            .Where(static reference => reference.Position == HistoricalEvidencePosition.Contradicts)
            .ToArray();
        if (fact.State == HistoricalFactState.Disputed)
        {
            bool hasSharedDisputedScope = supportingReferences.Any(supportingReference =>
                contradictingReferences.Any(contradictingReference =>
                    supportingReference.Scopes.Intersect(contradictingReference.Scopes).Any()));
            if (supportingReferences.Length == 0
                || contradictingReferences.Length == 0
                || !hasSharedDisputedScope)
            {
                throw Invalid(
                    HistoricalPersistenceErrorCodes.InvalidFactState,
                    "A disputed historical fact requires admissible evidence that conflicts on a shared assertion scope.");
            }

            return;
        }

        if (supportingReferences.Length == 0 || contradictingReferences.Length > 0)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A verified or probable historical fact requires supporting evidence without an admissible contradiction.");
        }
    }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
