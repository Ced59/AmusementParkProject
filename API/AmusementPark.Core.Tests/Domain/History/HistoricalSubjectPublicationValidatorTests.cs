using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalSubjectPublicationValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Validate_WhenPublishedFactFollowsHiddenSubject_ShouldRejectPublication()
    {
        HistoricalFact fact = CreateFact(HistoricalSubjectPublicationPolicy.FollowCurrentSubject);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic: false));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Fact]
    public void Validate_WhenHistoricalOnlyFactHasNoCurrentPublicSubject_ShouldAcceptPublication()
    {
        HistoricalFact fact = CreateFact(HistoricalSubjectPublicationPolicy.HistoricalOnly);

        HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic: false);
    }

    private static HistoricalFact CreateFact(HistoricalSubjectPublicationPolicy publicationPolicy)
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                publicationPolicy),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            HistoricalFactState.Verified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Published,
            HistoricalPublicationState.Published,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[] { new HistoricalSourceRevisionReference(Guid.NewGuid(), 1) },
            null,
            null,
            "history-opening-1998",
            RecordedAtUtc.AddMinutes(-2),
            RecordedAtUtc.AddMinutes(-1),
            "hist-v1",
            5,
            4,
            RecordedAtUtc);
    }
}
