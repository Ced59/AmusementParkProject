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
        HistoricalEvidencePosition position,
        IReadOnlyCollection<HistoricalSourceScope> scopes,
        string? historicalLabel,
        string? structuredValue,
        int? sequenceWithinDate,
        string? narrativeContentId,
        string? otherTypeLabel,
        LifecycleBoundaryMeaning? lifecycleBoundaryMeaning,
        HistoricalAttributeKind? attributeKind,
        AttributeBoundaryMeaning? attributeBoundaryMeaning)
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

        if (!Enum.IsDefined(subjectType)
            || !Enum.IsDefined(factType)
            || !Enum.IsDefined(position))
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
        string? normalizedHistoricalLabel = NormalizeOptional(historicalLabel, 300);
        string? normalizedStructuredValue = NormalizeOptional(structuredValue, 16000);
        string? normalizedNarrativeContentId = NormalizeOptional(narrativeContentId, 200);
        string? normalizedOtherTypeLabel = NormalizeOptional(otherTypeLabel, 200);
        ValidateScopedValue(
            normalizedScopes,
            HistoricalSourceScope.HistoricalLabel,
            normalizedHistoricalLabel is not null);
        ValidateScopedValue(
            normalizedScopes,
            HistoricalSourceScope.StructuredValue,
            normalizedStructuredValue is not null);
        ValidateScopedValue(
            normalizedScopes,
            HistoricalSourceScope.SequenceWithinDate,
            sequenceWithinDate.HasValue);
        ValidateScopedValue(
            normalizedScopes,
            HistoricalSourceScope.Narrative,
            normalizedNarrativeContentId is not null);
        bool requiresOtherTypeLabel = factType == HistoricalFactType.Other
            && normalizedScopes.Contains(HistoricalSourceScope.FactType);
        if (requiresOtherTypeLabel != (normalizedOtherTypeLabel is not null))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A citation for a custom historical fact type must bind its exact label.");
        }
        if (lifecycleBoundaryMeaning.HasValue && !Enum.IsDefined(lifecycleBoundaryMeaning.Value)
            || attributeKind.HasValue && !Enum.IsDefined(attributeKind.Value)
            || attributeBoundaryMeaning.HasValue && !Enum.IsDefined(attributeBoundaryMeaning.Value))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidEnum,
                "A historical source citation contains invalid boundary semantics.");
        }

        bool hasBoundarySemantics = lifecycleBoundaryMeaning.HasValue
            || attributeKind.HasValue
            || attributeBoundaryMeaning.HasValue;
        if (hasBoundarySemantics && !normalizedScopes.Contains(HistoricalSourceScope.Period))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "Historical boundary semantics require the period scope.");
        }

        this.SourceId = sourceId;
        this.Revision = revision;
        this.SubjectType = subjectType;
        this.SubjectId = normalizedSubjectId;
        this.FactType = factType;
        this.Period = period;
        this.Position = position;
        this.Scopes = Array.AsReadOnly(normalizedScopes);
        this.HistoricalLabel = normalizedHistoricalLabel;
        this.StructuredValue = normalizedStructuredValue;
        this.SequenceWithinDate = sequenceWithinDate;
        this.NarrativeContentId = normalizedNarrativeContentId;
        this.OtherTypeLabel = normalizedOtherTypeLabel;
        this.LifecycleBoundaryMeaning = lifecycleBoundaryMeaning;
        this.AttributeKind = attributeKind;
        this.AttributeBoundaryMeaning = attributeBoundaryMeaning;
    }

    public Guid SourceId { get; }

    public int Revision { get; }

    public HistoricalSubjectType SubjectType { get; }

    public string SubjectId { get; }

    public HistoricalFactType FactType { get; }

    public HistoricalPeriod Period { get; }

    public HistoricalEvidencePosition Position { get; }

    public IReadOnlyList<HistoricalSourceScope> Scopes { get; }

    public string? HistoricalLabel { get; }

    public string? StructuredValue { get; }

    public int? SequenceWithinDate { get; }

    public string? NarrativeContentId { get; }

    public string? OtherTypeLabel { get; }

    public LifecycleBoundaryMeaning? LifecycleBoundaryMeaning { get; }

    public HistoricalAttributeKind? AttributeKind { get; }

    public AttributeBoundaryMeaning? AttributeBoundaryMeaning { get; }

    private static string? NormalizeOptional(string? value, int maximumLength)
    {
        if (value is null)
        {
            return null;
        }

        string normalizedValue = value.Trim();
        if (normalizedValue.Length == 0
            || normalizedValue.Length > maximumLength
            || normalizedValue.Any(char.IsControl))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A scoped historical source value is invalid.");
        }

        return normalizedValue;
    }

    private static void ValidateScopedValue(
        IReadOnlyCollection<HistoricalSourceScope> scopes,
        HistoricalSourceScope scope,
        bool hasValue)
    {
        if (scopes.Contains(scope) != hasValue)
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "A historical source citation must persist every value-specific scope exactly.");
        }
    }
}
