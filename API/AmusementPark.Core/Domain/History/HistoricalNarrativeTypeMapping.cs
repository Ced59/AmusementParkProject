namespace AmusementPark.Core.Domain.History;

public sealed record HistoricalNarrativeTypeMapping(
    HistoricalFactType FactType,
    LifecycleBoundaryMeaning? LifecycleBoundaryMeaning,
    HistoricalAttributeKind? AttributeKind,
    AttributeBoundaryMeaning? AttributeBoundaryMeaning);
