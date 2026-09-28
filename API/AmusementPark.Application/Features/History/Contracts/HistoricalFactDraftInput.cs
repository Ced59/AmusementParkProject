using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalFactDraftInput(
    HistoricalSubjectType SubjectType,
    string SubjectId,
    HistoricalFactType Type,
    HistoricalPeriodInput Period,
    HistoricalFactState State,
    HistoricalImportance Importance,
    IReadOnlyCollection<HistoricalLocalizedText> PublicUncertaintyExplanation,
    LifecycleBoundaryMeaning? LifecycleBoundaryMeaning,
    HistoricalAttributeKind? AttributeKind,
    AttributeBoundaryMeaning? AttributeBoundaryMeaning,
    int? SequenceWithinDate,
    IReadOnlyCollection<HistoricalEvidenceSourceInput> Sources,
    string? StructuredValue,
    string? OtherTypeLabel,
    string? NarrativeContentId);
