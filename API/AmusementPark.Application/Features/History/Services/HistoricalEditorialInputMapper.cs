using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

public static class HistoricalEditorialInputMapper
{
    private static readonly HistoricalSourceScope[] RelationScopes =
    {
        HistoricalSourceScope.RelationSourceIdentity,
        HistoricalSourceScope.RelationTargetIdentity,
        HistoricalSourceScope.RelationType,
        HistoricalSourceScope.Period,
    };

    public static HistoricalPeriod ToPeriod(HistoricalPeriodInput input)
    {
        ArgumentNullException.ThrowIfNull(input);
        return new HistoricalPeriod(
            ToDate(input.Start),
            ToDate(input.End),
            input.StartConfidence,
            input.EndConfidence);
    }

    public static HistoricalSubject ResolveSubject(
        IReadOnlyCollection<HistoricalSubject> subjects,
        HistoricalSubjectType subjectType,
        string subjectId)
    {
        ArgumentNullException.ThrowIfNull(subjects);
        string normalizedSubjectId = subjectId?.Trim() ?? string.Empty;
        return subjects.FirstOrDefault(subject =>
                subject.Type == subjectType
                && string.Equals(subject.Id, normalizedSubjectId, StringComparison.Ordinal))
            ?? throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidIdentifier,
                "The selected historical subject does not belong to this park.",
                nameof(subjectId));
    }

    public static HistoricalSourceRevisionReference[] ToFactSourceReferences(
        HistoricalFactDraftInput input,
        HistoricalSubject subject,
        HistoricalPeriod period,
        IReadOnlyCollection<HistoricalSourceReference> sources)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(subject);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(sources);
        HistoricalSourceScope[] requiredScopes = BuildFactScopes(input);

        return input.Sources.Select(reference =>
        {
            HistoricalSourceReference source = ResolveSource(reference, sources);
            EnsureScopes(source, requiredScopes);
            return new HistoricalSourceRevisionReference(
                source.Id,
                source.Revision,
                subject.Type,
                subject.Id,
                input.Type,
                period,
                reference.Position,
                requiredScopes,
                subject.HistoricalLabel,
                input.StructuredValue,
                input.SequenceWithinDate,
                input.NarrativeContentId,
                input.OtherTypeLabel,
                input.LifecycleBoundaryMeaning,
                input.AttributeKind,
                input.AttributeBoundaryMeaning);
        }).ToArray();
    }

    public static HistoricalRelationSourceRevisionReference[] ToRelationSourceReferences(
        HistoricalRelationDraftInput input,
        HistoricalSubject sourceSubject,
        HistoricalSubject targetSubject,
        HistoricalPeriod period,
        IReadOnlyCollection<HistoricalSourceReference> sources)
    {
        ArgumentNullException.ThrowIfNull(input);
        ArgumentNullException.ThrowIfNull(sourceSubject);
        ArgumentNullException.ThrowIfNull(targetSubject);
        ArgumentNullException.ThrowIfNull(period);
        ArgumentNullException.ThrowIfNull(sources);
        HistoricalSubjectKey sourceKey = new HistoricalSubjectKey(
            sourceSubject.Type,
            sourceSubject.Id,
            sourceSubject.ContextParkId);
        HistoricalSubjectKey targetKey = new HistoricalSubjectKey(
            targetSubject.Type,
            targetSubject.Id,
            targetSubject.ContextParkId);

        return input.Sources.Select(reference =>
        {
            HistoricalSourceReference source = ResolveSource(reference, sources);
            EnsureScopes(source, RelationScopes);
            return new HistoricalRelationSourceRevisionReference(
                source.Id,
                source.Revision,
                sourceKey,
                targetKey,
                input.Type,
                period,
                reference.Position,
                RelationScopes);
        }).ToArray();
    }

    private static HistoricalDate? ToDate(HistoricalDateInput? input)
    {
        return input is null
            ? null
            : new HistoricalDate(
                input.Year,
                input.Month,
                input.Day,
                input.Precision,
                input.IsApproximate,
                input.Qualifier);
    }

    private static HistoricalSourceScope[] BuildFactScopes(HistoricalFactDraftInput input)
    {
        List<HistoricalSourceScope> scopes = new List<HistoricalSourceScope>
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        if (!string.IsNullOrWhiteSpace(input.StructuredValue))
        {
            scopes.Add(HistoricalSourceScope.StructuredValue);
        }

        if (input.SequenceWithinDate.HasValue)
        {
            scopes.Add(HistoricalSourceScope.SequenceWithinDate);
        }

        if (!string.IsNullOrWhiteSpace(input.NarrativeContentId))
        {
            scopes.Add(HistoricalSourceScope.Narrative);
        }

        return scopes.ToArray();
    }

    private static HistoricalSourceReference ResolveSource(
        HistoricalEvidenceSourceInput reference,
        IReadOnlyCollection<HistoricalSourceReference> sources)
    {
        return sources.FirstOrDefault(source =>
                source.Id == reference.SourceId && source.Revision == reference.Revision)
            ?? throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.MissingSource,
                "The selected immutable source revision does not exist.");
    }

    private static void EnsureScopes(
        HistoricalSourceReference source,
        IReadOnlyCollection<HistoricalSourceScope> requiredScopes)
    {
        if (requiredScopes.Any(scope => !source.Scopes.Contains(scope)))
        {
            throw new HistoricalPersistenceValidationException(
                HistoricalPersistenceErrorCodes.InvalidSourceScope,
                "The selected source does not declare every scope required by this assertion.");
        }
    }
}
