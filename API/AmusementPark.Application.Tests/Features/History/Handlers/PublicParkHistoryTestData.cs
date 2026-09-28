using AmusementPark.Application.Features.History.Ports;
using AmusementPark.Core.Domain.History;
using AmusementPark.Core.Domain.Parks;
using Moq;

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
        Guid? sourceId = null,
        string? narrativeContentId = null,
        HistoricalImportance importance = HistoricalImportance.Major)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForYear(year));
        HistoricalSourceRevisionReference sourceReference = CreateSourceReference(
            sourceId ?? Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            LifecycleBoundaryMeaning.FirstOperatingDay,
            narrativeContentId);
        return new HistoricalFact(
            factId ?? Guid.NewGuid(),
            subject,
            HistoricalFactType.Opening,
            period,
            HistoricalFactState.Verified,
            importance,
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
            narrativeContentId,
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

    public static IHistoricalSourceRepository CreatePublicSourceRepository(
        IReadOnlyCollection<HistoricalFact> facts)
    {
        HistoricalSourceReference[] sources = facts
            .Where(static fact => fact.SourceReferences.Count == 1)
            .Select(CreateSource)
            .ToArray();
        Mock<IHistoricalSourceRepository> repository = new(MockBehavior.Strict);
        if (sources.Length > 0)
        {
            repository
                .Setup(value => value.GetRevisionsAsync(
                    It.IsAny<IReadOnlyCollection<HistoricalSourceRevisionReference>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sources);
            repository
                .Setup(value => value.GetLatestRevisionsAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(sources);
        }

        return repository.Object;
    }

    private static HistoricalSourceRevisionReference CreateSourceReference(
        Guid sourceId,
        HistoricalSubject subject,
        HistoricalFactType factType,
        HistoricalPeriod period,
        LifecycleBoundaryMeaning lifecycleBoundaryMeaning,
        string? narrativeContentId)
    {
        HistoricalSourceScope[] scopes =
        {
            HistoricalSourceScope.SubjectIdentity,
            HistoricalSourceScope.HistoricalLabel,
            HistoricalSourceScope.FactType,
            HistoricalSourceScope.Period,
        };
        if (narrativeContentId is not null)
        {
            scopes = scopes.Append(HistoricalSourceScope.Narrative).ToArray();
        }
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
            narrativeContentId,
            null,
            lifecycleBoundaryMeaning,
            null,
            null);
    }
}
