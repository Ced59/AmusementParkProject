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

    [Theory]
    [InlineData(HistoricalSubjectType.ParkItem)]
    [InlineData(HistoricalSubjectType.ParkZone)]
    public void Validate_WhenHistoricalOnlyParkSubjectHasNoDurableParkScope_ShouldRejectPublication(
        HistoricalSubjectType subjectType)
    {
        HistoricalFact fact = CreateFact(
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            subjectType);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic: false));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidFactState, exception.ErrorCode);
    }

    [Theory]
    [InlineData(HistoricalSubjectType.ParkItem)]
    [InlineData(HistoricalSubjectType.ParkZone)]
    public void Validate_WhenHistoricalOnlyParkSubjectHasDurableParkScope_ShouldAcceptPublication(
        HistoricalSubjectType subjectType)
    {
        HistoricalFact fact = CreateFact(
            HistoricalSubjectPublicationPolicy.HistoricalOnly,
            subjectType,
            "park-1");

        HistoricalSubjectPublicationValidator.Validate(fact, currentSubjectIsPublic: false);
    }

    private static HistoricalFact CreateFact(
        HistoricalSubjectPublicationPolicy publicationPolicy,
        HistoricalSubjectType subjectType = HistoricalSubjectType.Park,
        string? contextParkId = null)
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));
        string subjectId = subjectType == HistoricalSubjectType.Park ? "park-1" : "item-1";
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                subjectType,
                subjectId,
                "Parc exemple",
                publicationPolicy,
                contextParkId),
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
            new[]
            {
                new HistoricalSourceRevisionReference(
                    Guid.NewGuid(),
                    1,
                    subjectType,
                    subjectId,
                    HistoricalFactType.Opening,
                    period,
                    HistoricalEvidencePosition.Supports,
                    new[]
                    {
                        HistoricalSourceScope.SubjectIdentity,
                        HistoricalSourceScope.HistoricalLabel,
                        HistoricalSourceScope.FactType,
                        HistoricalSourceScope.Period,
                    },
                    "Parc exemple",
                    null,
                    null,
                    null,
                    null,
                    LifecycleBoundaryMeaning.FirstOperatingDay,
                    null,
                    null),
            },
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
