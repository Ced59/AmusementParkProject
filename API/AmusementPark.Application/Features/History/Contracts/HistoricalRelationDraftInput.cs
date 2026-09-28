using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Contracts;

public sealed record HistoricalRelationDraftInput(
    HistoricalSubjectType SourceSubjectType,
    string SourceSubjectId,
    HistoricalSubjectType TargetSubjectType,
    string TargetSubjectId,
    HistoricalRelationType Type,
    HistoricalRelationDirection Direction,
    HistoricalPeriodInput Period,
    HistoricalFactState State,
    IReadOnlyCollection<HistoricalLocalizedText> PublicUncertaintyExplanation,
    IReadOnlyCollection<HistoricalEvidenceSourceInput> Sources,
    string? EditorialNote);
