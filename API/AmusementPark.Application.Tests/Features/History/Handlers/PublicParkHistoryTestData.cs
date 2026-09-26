using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;

namespace AmusementPark.Application.Tests.Features.History.Handlers;

internal static class PublicParkHistoryTestData
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 12, 0, 0, DateTimeKind.Utc);

    public static Park CreatePark(bool isVisible = true)
    {
        return new Park
        {
            Id = "park-1",
            Name = "Parc témoin",
            IsVisible = isVisible,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    public static ParkItem CreateParkItem(
        string id,
        string name,
        bool isVisible = true)
    {
        return new ParkItem
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            IsVisible = isVisible,
            AdminReviewStatus = AdminReviewStatus.Validated,
        };
    }

    public static ParkZone CreateParkZone(
        string id,
        string name,
        bool isVisible = true)
    {
        return new ParkZone
        {
            Id = id,
            ParkId = "park-1",
            Name = name,
            IsVisible = isVisible,
        };
    }

    public static HistoricalFact CreateOpeningFact(
        HistoricalSubject subject,
        int year,
        Guid? factId = null,
        Guid? sourceId = null)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(year));
        HistoricalSourceRevisionReference sourceReference = CreateSourceReference(
            sourceId ?? Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            LifecycleBoundaryMeaning.FirstOperatingDay);
        return new HistoricalFact(
            factId ?? Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[] { sourceReference },
            null,
            null,
            null,
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            ParkHistoricalSnapshotBuilder.CurrentMethodologyVersion,
            2,
            1,
            RecordedAtUtc);
    }

    public static HistoricalSourceReference CreateSource(HistoricalFact fact)
    {
        HistoricalSourceRevisionReference sourceReference = fact.SourceReferences.Single();
        return new HistoricalSourceReference(
            sourceReference.SourceId,
            sourceReference.Revision,
            HistoricalSourceType.OfficialWebsite,
            "Source publique",
            "Parc témoin",
            "https://example.com/history",
            null,
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 9, 20),
            "fr",
            null,
            sourceReference.Scopes,
            "Note strictement administrative",
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            RecordedAtUtc,
            HistoricalRevisionOrigin.Ordinary);
    }

    private static HistoricalSourceRevisionReference CreateSourceReference(
        Guid sourceId,
        HistoricalSubject subject,
        HistoricalFactType factType,
        HistoricalPeriod period,
        LifecycleBoundaryMeaning lifecycleBoundaryMeaning)
    {
        HistoricalSourceScope[] scopes =
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        return new HistoricalSourceRevisionReference(
            sourceId,
            2,
            subject.Type,
            subject.Id,
            factType,
            period,
            HistoricalEvidencePosition.Supports,
            scopes,
            subject.HistoricalLabel,
            null,
            null,
            null,
            null,
            lifecycleBoundaryMeaning,
            null,
            null);
    }
}
