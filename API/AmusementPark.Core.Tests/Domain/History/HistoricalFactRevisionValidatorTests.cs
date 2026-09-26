using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalFactRevisionValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidatePredecessor_WhenReferencedRevisionExists_ShouldAcceptCorrection()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(factId, 1, null, RecordedAtUtc.AddMinutes(-1));
        HistoricalFact correction = CreateFact(factId, 2, 1, RecordedAtUtc);

        HistoricalFactRevisionValidator.ValidatePredecessor(correction, predecessor);
    }

    [Fact]
    public void ValidatePredecessor_WhenReferencedRevisionIsMissing_ShouldRejectCorrection()
    {
        HistoricalFact correction = CreateFact(Guid.NewGuid(), 2, 1, RecordedAtUtc);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalFactRevisionValidator.ValidatePredecessor(correction, null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void ValidatePredecessor_WhenRevisionBelongsToAnotherFact_ShouldRejectCorrection()
    {
        HistoricalFact predecessor = CreateFact(Guid.NewGuid(), 1, null, RecordedAtUtc.AddMinutes(-1));
        HistoricalFact correction = CreateFact(Guid.NewGuid(), 2, 1, RecordedAtUtc);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalFactRevisionValidator.ValidatePredecessor(correction, predecessor));
    }

    private static HistoricalFact CreateFact(
        Guid factId,
        int revision,
        int? supersedesRevision,
        DateTime recordedAtUtc)
    {
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
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
            new[] { new HistoricalSourceRevisionReference(Guid.Parse("11111111-1111-1111-1111-111111111111"), 1) },
            null,
            null,
            "history-opening-1998",
            recordedAtUtc.AddMinutes(-2),
            recordedAtUtc.AddMinutes(-1),
            "hist-v1",
            revision,
            supersedesRevision,
            recordedAtUtc);
    }
}
