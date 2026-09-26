namespace AmusementPark.Core.Domain.History;

public sealed record LegacyHistoryEventTypeMapping(
    HistoricalFactType FactType,
    LifecycleBoundaryMeaning? LifecycleBoundaryMeaning,
    HistoricalAttributeKind? AttributeKind,
    AttributeBoundaryMeaning? AttributeBoundaryMeaning);
