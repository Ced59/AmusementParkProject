using System.Globalization;
using AmusementPark.Application.Features.History.Commands;
using AmusementPark.Application.Features.History.Contracts;
using AmusementPark.Application.Features.History.Results;
using AmusementPark.Core.Domain.History;
using AmusementPark.WebAPI.Contracts.History;

namespace AmusementPark.WebAPI.Mappers;

public static class HistoricalEditorialHttpMapper
{
    public static AdminHistoricalParkWorkbenchDto ToHttp(
        this AdminHistoricalParkWorkbenchResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new AdminHistoricalParkWorkbenchDto
        {
            ParkId = result.ParkId,
            ParkName = result.ParkName,
            Subjects = result.Subjects.Select(ToHttp).ToArray(),
            Facts = result.Facts.Select(ToHttp).ToArray(),
            Relations = result.Relations.Select(ToHttp).ToArray(),
            Sources = result.Sources.Select(ToHttp).ToArray(),
            Diagnostics = new AdminHistoricalParkDiagnosticsResult(
                result.ParkId,
                result.ParkName,
                result.Diagnostics,
                result.VisitCounts,
                result.RolloutGate).ToHttp(),
        };
    }

    public static HistoricalEditorialMutationDto ToHttp(
        this HistoricalEditorialMutationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new HistoricalEditorialMutationDto
        {
            ResourceType = result.ResourceType.ToString(),
            ResourceId = result.ResourceId.ToString("D", CultureInfo.InvariantCulture),
            Revision = result.Revision,
            WorkflowState = result.WorkflowState.ToString(),
            PublicationState = result.PublicationState.ToString(),
            FactState = result.FactState?.ToString(),
        };
    }

    public static HistoricalPublicationImpactPreviewDto ToHttp(
        this HistoricalPublicationImpactPreviewResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        return new HistoricalPublicationImpactPreviewDto
        {
            ResourceType = result.ResourceType.ToString(),
            ResourceId = result.ResourceId.ToString("D", CultureInfo.InvariantCulture),
            ResourceLabel = result.ResourceLabel,
            PreviewYear = result.PreviewYear,
            CanPublish = result.CanPublish,
            BlockingReasons = result.BlockingReasons,
            AffectedFromYear = result.AffectedFromYear,
            AffectedToYear = result.AffectedToYear,
            AffectedSnapshotYearCount = result.AffectedSnapshotYearCount,
            ChangedSubjectCount = result.ChangedSubjectCount,
            Before = ToHttp(result.Before),
            After = ToHttp(result.After),
            Visits = new AdminHistoricalVisitDiagnosticsDto
            {
                PotentiallyInconsistentVisitCount = result.VisitCounts.PotentiallyInconsistentVisitCount,
                ConfirmedConflictVisitCount = result.VisitCounts.ConfirmedConflictVisitCount,
                UnverifiedVisitCount = result.VisitCounts.UnverifiedVisitCount,
            },
        };
    }

    public static bool TryToCommand(
        this SaveHistoricalSourceRequestDto request,
        Guid? sourceId,
        string actorUserId,
        out SaveHistoricalSourceCommand? command)
    {
        command = null;
        if (!TryParse(request.Type, out HistoricalSourceType type)
            || !TryParse(request.Accessibility, out HistoricalSourceAccessibility accessibility)
            || !TryParseMany(request.Scopes, out HistoricalSourceScope[] scopes))
        {
            return false;
        }

        command = new SaveHistoricalSourceCommand(
            sourceId,
            request.ExpectedRevision,
            new HistoricalSourceDraftInput(
                type,
                request.Title,
                request.PublisherOrAuthor,
                request.Url,
                request.BibliographicReference,
                request.PublishedOn,
                request.AccessedOn,
                request.LanguageCode,
                request.ArchiveUrl,
                scopes,
                request.AdminNote,
                accessibility),
            actorUserId,
            request.ReviewNote);
        return true;
    }

    public static bool TryToCommand(
        this SaveHistoricalFactRequestDto request,
        string parkId,
        Guid? factId,
        string actorUserId,
        out SaveHistoricalFactCommand? command)
    {
        command = null;
        try
        {
            if (!TryParse(request.SubjectType, out HistoricalSubjectType subjectType)
                || !TryParse(request.Type, out HistoricalFactType type)
                || !TryParse(request.State, out HistoricalFactState state)
                || !TryParse(request.Importance, out HistoricalImportance importance)
                || !TryParseOptional(request.LifecycleBoundaryMeaning, out LifecycleBoundaryMeaning? lifecycleBoundary)
                || !TryParseOptional(request.AttributeKind, out HistoricalAttributeKind? attributeKind)
                || !TryParseOptional(request.AttributeBoundaryMeaning, out AttributeBoundaryMeaning? attributeBoundary)
                || !TryToPeriod(request.Period, out HistoricalPeriodInput? period)
                || !TryToEvidence(request.Sources, out HistoricalEvidenceSourceInput[] sources))
            {
                return false;
            }

            HistoricalLocalizedText[] explanations = request.PublicUncertaintyExplanation
                .Select(static item => new HistoricalLocalizedText(item.LanguageCode, item.Value))
                .ToArray();
            command = new SaveHistoricalFactCommand(
                parkId,
                factId,
                request.ExpectedRevision,
                new HistoricalFactDraftInput(
                    subjectType,
                    request.SubjectId,
                    type,
                    period!,
                    state,
                    importance,
                    explanations,
                    lifecycleBoundary,
                    attributeKind,
                    attributeBoundary,
                    request.SequenceWithinDate,
                    sources,
                    request.StructuredValue,
                    request.OtherTypeLabel,
                    request.NarrativeContentId,
                    request.SubjectContextParkId),
                actorUserId,
                request.ReviewNote);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    public static bool TryToCommand(
        this SaveHistoricalRelationRequestDto request,
        string parkId,
        Guid? relationId,
        string actorUserId,
        out SaveHistoricalRelationCommand? command)
    {
        command = null;
        try
        {
            if (!TryParse(request.SourceSubjectType, out HistoricalSubjectType sourceSubjectType)
                || !TryParse(request.TargetSubjectType, out HistoricalSubjectType targetSubjectType)
                || !TryParse(request.Type, out HistoricalRelationType type)
                || !TryParse(request.Direction, out HistoricalRelationDirection direction)
                || !TryParse(request.State, out HistoricalFactState state)
                || !TryToPeriod(request.Period, out HistoricalPeriodInput? period)
                || !TryToEvidence(request.Sources, out HistoricalEvidenceSourceInput[] sources))
            {
                return false;
            }

            HistoricalLocalizedText[] explanations = request.PublicUncertaintyExplanation
                .Select(static item => new HistoricalLocalizedText(item.LanguageCode, item.Value))
                .ToArray();
            command = new SaveHistoricalRelationCommand(
                parkId,
                relationId,
                request.ExpectedRevision,
                new HistoricalRelationDraftInput(
                    sourceSubjectType,
                    request.SourceSubjectId,
                    targetSubjectType,
                    request.TargetSubjectId,
                    type,
                    direction,
                    period!,
                    state,
                    explanations,
                    sources,
                    request.EditorialNote,
                    request.SourceSubjectContextParkId,
                    request.TargetSubjectContextParkId),
                actorUserId,
                request.ReviewNote);
            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static AdminHistoricalSubjectDto ToHttp(HistoricalSubject subject)
    {
        return new AdminHistoricalSubjectDto
        {
            Type = subject.Type.ToString(),
            Id = subject.Id,
            ContextParkId = subject.ContextParkId,
            Label = subject.HistoricalLabel,
            PublicationPolicy = subject.PublicationPolicy.ToString(),
        };
    }

    private static HistoricalSnapshotImpactSummaryDto ToHttp(
        HistoricalSnapshotImpactSummary summary)
    {
        return new HistoricalSnapshotImpactSummaryDto
        {
            KnownOpenSubjectCount = summary.KnownOpenSubjectCount,
            AmbiguityCount = summary.AmbiguityCount,
            ReliablePeriodSubjectCount = summary.ReliablePeriodSubjectCount,
            PartialPeriodSubjectCount = summary.PartialPeriodSubjectCount,
            UndatedSubjectCount = summary.UndatedSubjectCount,
        };
    }

    private static AdminHistoricalFactDto ToHttp(HistoricalFact fact)
    {
        return new AdminHistoricalFactDto
        {
            Id = fact.Id.ToString("D", CultureInfo.InvariantCulture),
            Revision = fact.Revision,
            Subject = ToHttp(fact.Subject),
            Type = fact.Type.ToString(),
            Period = ToHttp(fact.Period),
            State = fact.State.ToString(),
            Importance = fact.Importance.ToString(),
            WorkflowState = fact.WorkflowState.ToString(),
            PublicationState = fact.PublicationState.ToString(),
            PublicUncertaintyExplanation = fact.PublicUncertaintyExplanation
                .Select(ToHttp)
                .ToArray(),
            LifecycleBoundaryMeaning = fact.LifecycleBoundaryMeaning?.ToString(),
            AttributeKind = fact.AttributeKind?.ToString(),
            AttributeBoundaryMeaning = fact.AttributeBoundaryMeaning?.ToString(),
            SequenceWithinDate = fact.SequenceWithinDate,
            Sources = fact.SourceReferences.Select(reference => ToHttp(
                reference.SourceId,
                reference.Revision,
                reference.Position)).ToArray(),
            StructuredValue = fact.StructuredValue,
            OtherTypeLabel = fact.OtherTypeLabel,
            NarrativeContentId = fact.NarrativeContentId,
        };
    }

    private static AdminHistoricalRelationDto ToHttp(HistoricalRelation relation)
    {
        return new AdminHistoricalRelationDto
        {
            Id = relation.Id.ToString("D", CultureInfo.InvariantCulture),
            Revision = relation.Revision,
            Source = ToHttp(relation.Source),
            Target = ToHttp(relation.Target),
            Type = relation.Type.ToString(),
            Direction = relation.Direction.ToString(),
            Period = ToHttp(relation.Period),
            State = relation.State.ToString(),
            WorkflowState = relation.WorkflowState.ToString(),
            PublicationState = relation.PublicationState.ToString(),
            PublicUncertaintyExplanation = relation.PublicUncertaintyExplanation
                .Select(ToHttp)
                .ToArray(),
            Sources = relation.SourceReferences.Select(reference => ToHttp(
                reference.SourceId,
                reference.Revision,
                reference.Position)).ToArray(),
            EditorialNote = relation.EditorialNote,
        };
    }

    private static AdminHistoricalSourceDto ToHttp(HistoricalSourceReference source)
    {
        return new AdminHistoricalSourceDto
        {
            Id = source.Id.ToString("D", CultureInfo.InvariantCulture),
            Revision = source.Revision,
            Type = source.Type.ToString(),
            Title = source.Title,
            PublisherOrAuthor = source.PublisherOrAuthor,
            Url = source.Url,
            BibliographicReference = source.BibliographicReference,
            PublishedOn = source.PublishedOn,
            AccessedOn = source.AccessedOn,
            LanguageCode = source.LanguageCode,
            ArchiveUrl = source.ArchiveUrl,
            Scopes = source.Scopes.Select(static scope => scope.ToString()).ToArray(),
            AdminNote = source.AdminNote,
            Accessibility = source.Accessibility.ToString(),
            WorkflowState = source.WorkflowState.ToString(),
            PublicationState = source.PublicationState.ToString(),
        };
    }

    private static AdminHistoricalPeriodDto ToHttp(HistoricalPeriod period)
    {
        return new AdminHistoricalPeriodDto
        {
            Start = period.Start is null ? null : ToHttp(period.Start),
            End = period.End is null ? null : ToHttp(period.End),
            StartConfidence = period.StartConfidence.ToString(),
            EndConfidence = period.EndConfidence.ToString(),
        };
    }

    private static AdminHistoricalDateDto ToHttp(HistoricalDate date)
    {
        return new AdminHistoricalDateDto
        {
            Year = date.Year,
            Month = date.Month,
            Day = date.Day,
            Precision = date.Precision.ToString(),
            IsApproximate = date.IsApproximate,
            Qualifier = date.Qualifier?.ToString(),
        };
    }

    private static AdminHistoricalLocalizedTextDto ToHttp(HistoricalLocalizedText text)
    {
        return new AdminHistoricalLocalizedTextDto
        {
            LanguageCode = text.LanguageCode,
            Value = text.Value,
        };
    }

    private static AdminHistoricalEvidenceDto ToHttp(
        Guid sourceId,
        int revision,
        HistoricalEvidencePosition position)
    {
        return new AdminHistoricalEvidenceDto
        {
            SourceId = sourceId.ToString("D", CultureInfo.InvariantCulture),
            Revision = revision,
            Position = position.ToString(),
        };
    }

    private static bool TryToPeriod(
        HistoricalPeriodRequestDto request,
        out HistoricalPeriodInput? period)
    {
        period = null;
        if (!TryParse(request.StartConfidence, out PeriodBoundaryConfidence startConfidence)
            || !TryParse(request.EndConfidence, out PeriodBoundaryConfidence endConfidence)
            || !TryToDate(request.Start, out HistoricalDateInput? start)
            || !TryToDate(request.End, out HistoricalDateInput? end))
        {
            return false;
        }

        period = new HistoricalPeriodInput(start, end, startConfidence, endConfidence);
        return true;
    }

    private static bool TryToDate(
        HistoricalDateRequestDto? request,
        out HistoricalDateInput? date)
    {
        date = null;
        if (request is null)
        {
            return true;
        }

        if (!TryParse(request.Precision, out HistoryDatePrecision precision)
            || !TryParseOptional(request.Qualifier, out DateQualifier? qualifier))
        {
            return false;
        }

        date = new HistoricalDateInput(
            request.Year,
            request.Month,
            request.Day,
            precision,
            request.IsApproximate,
            qualifier);
        return true;
    }

    private static bool TryToEvidence(
        IReadOnlyCollection<HistoricalEvidenceSourceRequestDto> requests,
        out HistoricalEvidenceSourceInput[] sources)
    {
        List<HistoricalEvidenceSourceInput> values = new List<HistoricalEvidenceSourceInput>();
        foreach (HistoricalEvidenceSourceRequestDto request in requests)
        {
            if (!Guid.TryParse(request.SourceId, out Guid sourceId)
                || sourceId == Guid.Empty
                || request.Revision < 1
                || !TryParse(request.Position, out HistoricalEvidencePosition position))
            {
                sources = Array.Empty<HistoricalEvidenceSourceInput>();
                return false;
            }

            values.Add(new HistoricalEvidenceSourceInput(sourceId, request.Revision, position));
        }

        sources = values.ToArray();
        return true;
    }

    private static bool TryParse<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        return Enum.TryParse(value?.Trim(), true, out parsed) && Enum.IsDefined(parsed);
    }

    private static bool TryParseOptional<TEnum>(string? value, out TEnum? parsed)
        where TEnum : struct, Enum
    {
        parsed = null;
        if (string.IsNullOrWhiteSpace(value))
        {
            return true;
        }

        if (!TryParse(value, out TEnum candidate))
        {
            return false;
        }

        parsed = candidate;
        return true;
    }

    private static bool TryParseMany<TEnum>(
        IReadOnlyCollection<string> values,
        out TEnum[] parsed)
        where TEnum : struct, Enum
    {
        List<TEnum> items = new List<TEnum>();
        foreach (string value in values)
        {
            if (!TryParse(value, out TEnum item))
            {
                parsed = Array.Empty<TEnum>();
                return false;
            }

            items.Add(item);
        }

        parsed = items.Distinct().ToArray();
        return parsed.Length > 0;
    }
}
