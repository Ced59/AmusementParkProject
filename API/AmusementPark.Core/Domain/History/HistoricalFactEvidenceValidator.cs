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

        bool requiresAdmissibleEvidence = fact.State == HistoricalFactState.Verified
            || fact.PublicationState == HistoricalPublicationState.Published;
        if (!requiresAdmissibleEvidence)
        {
            return;
        }

        bool allSourcesAreAdmissible = sources.All(static source =>
            source.PublicationState == HistoricalPublicationState.Published
            && (source.WorkflowState is HistoricalEditorialWorkflowState.Published
                or HistoricalEditorialWorkflowState.Corrected)
            && (source.Accessibility is HistoricalSourceAccessibility.Accessible
                or HistoricalSourceAccessibility.Archived));
        if (!allSourcesAreAdmissible)
        {
            throw Invalid(
                HistoricalPersistenceErrorCodes.InvalidFactState,
                "A verified or published historical fact requires published and accessible evidence revisions.");
        }

        HashSet<HistoricalSourceScope> coveredScopes = sources
            .SelectMany(static source => source.Scopes)
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

        return scopes.ToArray();
    }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
