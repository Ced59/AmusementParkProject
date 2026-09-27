namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalRelationSourceRevisionReference
{
    public HistoricalRelationSourceRevisionReference(
        Guid sourceId,
        int revision,
        HistoricalSubjectKey sourceSubject,
        HistoricalSubjectKey targetSubject,
        HistoricalRelationType relationType,
        HistoricalPeriod period,
        HistoricalEvidencePosition position,
        IReadOnlyCollection<HistoricalSourceScope> scopes)
    {
        if (sourceId == Guid.Empty)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidIdentifier, "A relation source requires an identifier.");
        }

        if (revision < 1)
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidRevision, "A relation source requires a positive revision.");
        }

        ArgumentNullException.ThrowIfNull(sourceSubject);
        ArgumentNullException.ThrowIfNull(targetSubject);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(scopes);
        if (!Enum.IsDefined(relationType)
            || !Enum.IsDefined(position)
            || scopes.Count == 0
            || scopes.Any(static scope => !Enum.IsDefined(scope)))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidSourceScope, "A relation source contains an invalid assertion.");
        }

        HistoricalSourceScope[] normalizedScopes = scopes.Distinct().OrderBy(static scope => scope).ToArray();
        HistoricalSourceScope[] allowedScopes =
        {
            HistoricalSourceScope.RelationSourceIdentity,
            HistoricalSourceScope.RelationTargetIdentity,
            HistoricalSourceScope.RelationType,
            HistoricalSourceScope.Period,
        };
        if (normalizedScopes.Any(scope => !allowedScopes.Contains(scope)))
        {
            throw Invalid(HistoricalPersistenceErrorCodes.InvalidSourceScope, "A relation source uses a fact-only scope.");
        }

        this.SourceId = sourceId;
        this.Revision = revision;
        this.SourceSubject = sourceSubject;
        this.TargetSubject = targetSubject;
        this.RelationType = relationType;
        this.Period = period;
        this.Position = position;
        this.Scopes = Array.AsReadOnly(normalizedScopes);
    }

    public Guid SourceId { get; }

    public int Revision { get; }

    public HistoricalSubjectKey SourceSubject { get; }

    public HistoricalSubjectKey TargetSubject { get; }

    public HistoricalRelationType RelationType { get; }

    public HistoricalPeriod Period { get; }

    public HistoricalEvidencePosition Position { get; }

    public IReadOnlyList<HistoricalSourceScope> Scopes { get; }

    private static HistoricalPersistenceValidationException Invalid(string code, string message)
    {
        return new HistoricalPersistenceValidationException(code, message);
    }
}
