namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalSourceRevisionReference
{
    public HistoricalSourceRevisionReference(
        Guid sourceId,
        int revision,
        HistoricalSubjectType subjectType,
        string subjectId,
        HistoricalFactType factType,
        HistoricalPeriod period,
        IReadOnlyCollection<HistoricalSourceScope> scopes)
    {
        if (sourceId == Guid.Empty)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical source revision reference requires an identifier.");
        }

        if (revision < 1)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidRevision,
                "A historical source revision reference requires a positive revision.");
        }

        if (!Enum.IsDefined(subjectType) || !Enum.IsDefined(factType))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidEnum,
                "A historical source revision reference requires a valid assertion type.");
        }

        string normalizedSubjectId = subjectId?.Trim() ?? string.Empty;
        if (normalizedSubjectId.Length == 0
            || normalizedSubjectId.Length > 200
            || normalizedSubjectId.Any(char.IsControl))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "A historical source revision reference requires a valid assertion subject.",
                nameof(subjectId));
        }

        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(scopes);
        if (scopes.Count == 0
            || scopes.Any(static scope => !Enum.IsDefined(scope)))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A historical source revision reference requires valid assertion scopes.");
        }

        HistoricalSourceScope[] normalizedScopes = scopes
            .Distinct()
            .OrderBy(static scope => scope)
            .ToArray();

        this.SourceId = sourceId;
        this.Revision = revision;
        this.SubjectType = subjectType;
        this.SubjectId = normalizedSubjectId;
        this.FactType = factType;
        this.Period = period;
        this.Scopes = Array.AsReadOnly(normalizedScopes);
    }

    public Guid SourceId { get; }

    public int Revision { get; }

    public HistoricalSubjectType SubjectType { get; }

    public string SubjectId { get; }

    public HistoricalFactType FactType { get; }

    public HistoricalPeriod Period { get; }

    public IReadOnlyList<HistoricalSourceScope> Scopes { get; }
}
