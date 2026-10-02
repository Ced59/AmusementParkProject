using AmusementPark.Core.Domain.History;

namespace AmusementPark.Application.Features.History.Services;

internal sealed record HistoricalSourcePlan(
    HistoricalSourceReference DraftSource,
    HistoricalSourceReference PublishedSource,
    HistoricalSubject Subject,
    LegacyHistoryEventTypeMapping Mapping,
    HistoricalPeriod Period,
    string? StructuredValue,
    string? OtherTypeLabel,
    string NarrativeContentId)
{
    public HistoricalSourceRevisionReference CreateReference(int revision)
    {
        HistoricalSourceReference source = revision == this.PublishedSource.Revision
            ? this.PublishedSource
            : this.DraftSource;
        return new HistoricalSourceRevisionReference(
            source.Id,
            revision,
            this.Subject.Type,
            this.Subject.Id,
            this.Mapping.FactType,
            this.Period,
            HistoricalEvidencePosition.Supports,
            source.Scopes,
            this.Subject.HistoricalLabel,
            this.StructuredValue,
            null,
            this.NarrativeContentId,
            this.OtherTypeLabel,
            this.Mapping.LifecycleBoundaryMeaning,
            this.Mapping.AttributeKind,
            this.Mapping.AttributeBoundaryMeaning);
    }
}
