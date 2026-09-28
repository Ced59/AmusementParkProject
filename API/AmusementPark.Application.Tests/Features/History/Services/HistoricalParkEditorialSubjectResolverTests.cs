using AmusementPark.Application.Features.History.Services;
using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Application.Tests.Features.History.Services;

public sealed class HistoricalParkEditorialSubjectResolverTests
{
    [Fact]
    public void Merge_ShouldKeepHistoricalOnlySubjectsSelectable()
    {
        HistoricalSubject currentSubject = CreateSubject(
            "item-current",
            HistoricalSubjectPublicationPolicy.FollowCurrentSubject);
        HistoricalSubject historicalSubject = CreateSubject(
            "item-retired",
            HistoricalSubjectPublicationPolicy.HistoricalOnly);
        HistoricalRelation relation = new HistoricalRelation(
            Guid.NewGuid(),
            historicalSubject,
            currentSubject,
            HistoricalRelationType.ReplacedBy,
            HistoricalRelationDirection.Directed,
            HistoricalPeriod.Point(HistoricalDate.ForYear(2001)),
            HistoricalFactState.Unverified,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            Array.Empty<HistoricalRelationSourceRevisionReference>(),
            null,
            null,
            null,
            null,
            1,
            null,
            new DateTime(2026, 9, 28, 10, 0, 0, DateTimeKind.Utc));

        IReadOnlyCollection<HistoricalSubject> subjects =
            HistoricalParkEditorialSubjectResolver.Merge(
                new[] { currentSubject },
                Array.Empty<HistoricalFact>(),
                new[] { relation });

        Assert.Equal(2, subjects.Count);
        Assert.Contains(subjects, subject =>
            subject.Id == historicalSubject.Id
            && subject.PublicationPolicy == HistoricalSubjectPublicationPolicy.HistoricalOnly);
    }

    private static HistoricalSubject CreateSubject(
        string id,
        HistoricalSubjectPublicationPolicy publicationPolicy)
    {
        return new HistoricalSubject(
            HistoricalSubjectType.ParkItem,
            id,
            id,
            publicationPolicy,
            "park-1");
    }
}
