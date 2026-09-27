namespace AmusementPark.Core.Domain.History;

public static class HistoricalRelationEvidenceValidator
{
    private static readonly HistoricalSourceScope[] CoreScopes =
    {
        HistoricalSourceScope.RelationSourceIdentity,
        HistoricalSourceScope.RelationTargetIdentity,
        HistoricalSourceScope.RelationType,
        HistoricalSourceScope.Period,
    };

    public static void Validate(
        HistoricalRelation relation,
        IReadOnlyCollection<HistoricalSourceReference> resolvedSources)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(resolvedSources);
        HistoricalSourceReference[] sources = resolvedSources.ToArray();
        bool exactReferencesMatch = sources.Length == relation.SourceReferences.Count
            && sources.Select(static source => (source.Id, source.Revision)).Distinct().Count() == sources.Length
            && relation.SourceReferences.All(reference => sources.Any(source =>
                source.Id == reference.SourceId && source.Revision == reference.Revision));
        if (!exactReferencesMatch)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.MissingSource, "Every relation source revision must resolve exactly.");
        }

        HistoricalSubjectKey sourceKey = new(relation.Source.Type, relation.Source.Id);
        HistoricalSubjectKey targetKey = new(relation.Target.Type, relation.Target.Id);
        bool citationsAreBound = relation.SourceReferences.All(reference =>
            reference.SourceSubject == sourceKey
            && reference.TargetSubject == targetKey
            && reference.RelationType == relation.Type
            && reference.Period == relation.Period
            && reference.Scopes.All(scope => sources.Single(source =>
                    source.Id == reference.SourceId && source.Revision == reference.Revision)
                .Scopes.Contains(scope)));
        if (!citationsAreBound)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidSourceScope, "A relation citation is not bound to its exact assertion.");
        }

        if (relation.State != HistoricalFactState.Verified
            && relation.PublicationState != HistoricalPublicationState.Published)
        {
            return;
        }

        HashSet<(Guid Id, int Revision)> admissibleKeys = GetAdmissiblePublicSourceKeys(sources);
        HistoricalRelationSourceRevisionReference[] admissible = relation.SourceReferences
            .Where(reference => admissibleKeys.Contains((reference.SourceId, reference.Revision)))
            .ToArray();
        HistoricalRelationSourceRevisionReference[] supporting = admissible
            .Where(static reference => reference.Position == HistoricalEvidencePosition.Supports)
            .ToArray();
        HistoricalRelationSourceRevisionReference[] contradicting = admissible
            .Where(static reference => reference.Position == HistoricalEvidencePosition.Contradicts)
            .ToArray();
        bool oneSourceCoversRelation = supporting.Any(reference =>
            CoreScopes.All(scope => reference.Scopes.Contains(scope)));
        bool positionsAreValid = relation.State == HistoricalFactState.Disputed
            ? supporting.Length > 0
                && contradicting.Length > 0
                && supporting.Any(left => contradicting.Any(right => left.Scopes.Intersect(right.Scopes).Any()))
            : supporting.Length > 0 && contradicting.Length == 0;
        if (!oneSourceCoversRelation || !positionsAreValid)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRelation, "A published relation requires explicit admissible evidence for the complete link.");
        }
    }

    public static bool HasAdmissiblePublicSupport(
        HistoricalRelation relation,
        IReadOnlyCollection<HistoricalSourceReference> resolvedSources)
    {
        ArgumentNullException.ThrowIfNull(relation);
        ArgumentNullException.ThrowIfNull(resolvedSources);
        HashSet<(Guid Id, int Revision)> admissibleKeys = GetAdmissiblePublicSourceKeys(resolvedSources);
        return relation.SourceReferences.Any(reference =>
            reference.Position == HistoricalEvidencePosition.Supports
            && admissibleKeys.Contains((reference.SourceId, reference.Revision))
            && CoreScopes.All(scope => reference.Scopes.Contains(scope)));
    }

    public static IReadOnlyCollection<HistoricalSourceReference> FilterCurrentlyAdmissiblePublicSources(
        IReadOnlyCollection<HistoricalSourceReference> resolvedRevisions,
        IReadOnlyCollection<HistoricalSourceReference> latestRevisions)
    {
        ArgumentNullException.ThrowIfNull(resolvedRevisions);
        ArgumentNullException.ThrowIfNull(latestRevisions);
        HashSet<Guid> currentlyAdmissibleSourceIds = latestRevisions
            .Where(IsAdmissiblePublicSource)
            .Select(static source => source.Id)
            .ToHashSet();
        return resolvedRevisions
            .Where(source => IsAdmissiblePublicSource(source)
                && currentlyAdmissibleSourceIds.Contains(source.Id))
            .ToArray();
    }

    private static HashSet<(Guid Id, int Revision)> GetAdmissiblePublicSourceKeys(
        IEnumerable<HistoricalSourceReference> sources)
    {
        return sources
            .Where(IsAdmissiblePublicSource)
            .Select(static source => (source.Id, source.Revision))
            .ToHashSet();
    }

    private static bool IsAdmissiblePublicSource(HistoricalSourceReference source)
    {
        return source.PublicationState == HistoricalPublicationState.Published
            && source.WorkflowState is HistoricalEditorialWorkflowState.Published
                or HistoricalEditorialWorkflowState.Corrected
            && source.Accessibility is HistoricalSourceAccessibility.Accessible
                or HistoricalSourceAccessibility.Archived;
    }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
