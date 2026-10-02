using System.Text.Json;
using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

internal sealed class HistoricalNarrativeCanonicalFactFactory
{
    internal HistoricalPeriod BuildPeriod(HistoryEvent historyEvent)
    {
        HistoricalDate date = historyEvent.DatePrecision switch
        {
            HistoryDatePrecision.Year => HistoricalDate.ForYear(historyEvent.Year),
            HistoryDatePrecision.Month => HistoricalDate.ForMonth(historyEvent.Year, historyEvent.Month ?? 0),
            HistoryDatePrecision.Day => HistoricalDate.ForDay(
                historyEvent.Year,
                historyEvent.Month ?? 0,
                historyEvent.Day ?? 0),
            _ => throw new HistoricalTemporalValidationException(
                HistoricalTemporalErrorCodes.InvalidPrecision,
                "The historical date precision is invalid.",
                nameof(historyEvent.DatePrecision)),
        };
        return HistoricalPeriod.Point(date);
    }

    internal HistoricalSubject BuildSubject(
        HistoryEvent historyEvent,
        HistoricalSubjectResolution subjectResolution)
    {
        return new HistoricalSubject(
            subjectResolution.SubjectType,
            subjectResolution.SubjectId,
            ResolveLabel(historyEvent, subjectResolution.Label),
            subjectResolution.PublicationPolicy,
            subjectResolution.ContextParkId);
    }

    internal static string? BuildStructuredValue(
        HistoryEvent historyEvent,
        LegacyHistoryEventTypeMapping mapping)
    {
        if (!mapping.AttributeKind.HasValue)
        {
            return null;
        }

        string? requiredNextKey = mapping.AttributeKind.Value switch
        {
            HistoricalAttributeKind.Name or HistoricalAttributeKind.MarketPositioning
                or HistoricalAttributeKind.Theme => "next",
            HistoricalAttributeKind.Logo => "nextImageId",
            HistoricalAttributeKind.Operator => "nextId",
            _ => null,
        };
        Dictionary<string, string?> values = mapping.AttributeKind.Value switch
        {
            HistoricalAttributeKind.Name or HistoricalAttributeKind.MarketPositioning
                or HistoricalAttributeKind.Theme => new Dictionary<string, string?>
                {
                    ["previous"] = historyEvent.PreviousName,
                    ["next"] = historyEvent.NewName,
                },
            HistoricalAttributeKind.Logo => new Dictionary<string, string?>
                {
                    ["previousImageId"] = historyEvent.PreviousLogoImageId,
                    ["nextImageId"] = historyEvent.NewLogoImageId,
                },
            HistoricalAttributeKind.Operator => new Dictionary<string, string?>
                {
                    ["previousId"] = historyEvent.PreviousOperatorId,
                    ["nextId"] = historyEvent.NewOperatorId,
                },
            _ => new Dictionary<string, string?>(),
        };
        Dictionary<string, string> normalized = values
            .Where(static pair => !string.IsNullOrWhiteSpace(pair.Value))
            .ToDictionary(
                static pair => pair.Key,
                static pair => pair.Value!.Trim(),
                StringComparer.Ordinal);
        return normalized.Count == 0
            || requiredNextKey is null
            || !normalized.ContainsKey(requiredNextKey)
                ? null
                : JsonSerializer.Serialize(normalized);
    }

    internal HistoricalFact CreateDraft(
        Guid factId,
        HistoryEvent historyEvent,
        HistoricalSubject subject,
        LegacyHistoryEventTypeMapping mapping,
        HistoricalPeriod period,
        string? structuredValue,
        string? otherTypeLabel,
        IReadOnlyCollection<HistoricalSourceRevisionReference> sourceReferences,
        bool publish,
        DateTime recordedAtUtc)
    {
        return new HistoricalFact(
            factId,
            subject,
            mapping.FactType,
            period,
            publish ? HistoricalFactState.Probable : HistoricalFactState.Unverified,
            historyEvent.IsMajor ? HistoricalImportance.Major : HistoricalImportance.Standard,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            publish ? BuildProbabilityExplanations() : Array.Empty<HistoricalLocalizedText>(),
            mapping.LifecycleBoundaryMeaning,
            mapping.AttributeKind,
            mapping.AttributeBoundaryMeaning,
            null,
            sourceReferences,
            structuredValue,
            otherTypeLabel,
            historyEvent.Id,
            null,
            null,
            null,
            1,
            null,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    internal HistoricalFact CreateRevision(
        HistoricalFact previous,
        HistoricalEditorialWorkflowState stage,
        DateTime recordedAtUtc)
    {
        bool isPublished = stage == HistoricalEditorialWorkflowState.Published;
        return new HistoricalFact(
            previous.Id,
            previous.Subject,
            previous.Type,
            previous.Period,
            previous.State,
            previous.Importance,
            stage,
            isPublished ? HistoricalPublicationState.Published : HistoricalPublicationState.Draft,
            previous.PublicUncertaintyExplanation,
            previous.LifecycleBoundaryMeaning,
            previous.AttributeKind,
            previous.AttributeBoundaryMeaning,
            previous.SequenceWithinDate,
            previous.SourceReferences,
            previous.StructuredValue,
            previous.OtherTypeLabel,
            previous.NarrativeContentId,
            null,
            isPublished ? recordedAtUtc : null,
            isPublished ? HistoricalNarrativeCanonicalizationPolicy.MethodologyVersion : null,
            previous.Revision + 1,
            previous.Revision,
            recordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static IReadOnlyCollection<HistoricalLocalizedText> BuildProbabilityExplanations()
    {
        return new[]
        {
            new HistoricalLocalizedText("fr", "Cette information est considérée comme probable au vu des sources disponibles."),
            new HistoricalLocalizedText("en", "This information is considered probable based on the available sources."),
            new HistoricalLocalizedText("de", "Diese Information gilt anhand der verfügbaren Quellen als wahrscheinlich."),
            new HistoricalLocalizedText("nl", "Deze informatie wordt op basis van de beschikbare bronnen als waarschijnlijk beschouwd."),
            new HistoricalLocalizedText("it", "Questa informazione è considerata probabile in base alle fonti disponibili."),
            new HistoricalLocalizedText("es", "Esta información se considera probable según las fuentes disponibles."),
            new HistoricalLocalizedText("pl", "Ta informacja jest uznawana za prawdopodobną na podstawie dostępnych źródeł."),
            new HistoricalLocalizedText("pt", "Esta informação é considerada provável com base nas fontes disponíveis."),
        };
    }

    private static string ResolveLabel(HistoryEvent historyEvent, string fallback)
    {
        string label = new[]
            {
                historyEvent.NewName,
                historyEvent.PreviousName,
                fallback,
            }
            .FirstOrDefault(static value => !string.IsNullOrWhiteSpace(value))?
            .Trim() ?? fallback;
        return label.Length <= 300 ? label : label[..300];
    }
}
