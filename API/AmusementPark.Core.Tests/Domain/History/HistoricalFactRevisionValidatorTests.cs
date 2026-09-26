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
        HistoricalFact predecessor = CreateFact(factId, 5, 4, RecordedAtUtc.AddMinutes(-1));
        HistoricalFact correction = CreateFact(
            factId,
            6,
            5,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Corrected,
            HistoricalPublicationState.Published);

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
        HistoricalFact predecessor = CreateFact(Guid.NewGuid(), 5, 4, RecordedAtUtc.AddMinutes(-1));
        HistoricalFact correction = CreateFact(Guid.NewGuid(), 6, 5, RecordedAtUtc);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalFactRevisionValidator.ValidatePredecessor(correction, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenCorrectionFollowsDraft_ShouldRejectTransition()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(
            factId,
            1,
            null,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);
        HistoricalFact correction = CreateFact(
            factId,
            2,
            1,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Corrected,
            HistoricalPublicationState.Published);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalFactRevisionValidator.ValidatePredecessor(correction, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenPublishedFactReturnsToDraft_ShouldRejectTransition()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(factId, 5, 4, RecordedAtUtc.AddMinutes(-1));
        HistoricalFact draft = CreateFact(
            factId,
            6,
            5,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalFactRevisionValidator.ValidatePredecessor(draft, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenFactSkipsWorkflowStage_ShouldRejectTransition()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(
            factId,
            1,
            null,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);
        HistoricalFact structuredValidation = CreateFact(
            factId,
            2,
            1,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.Draft);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalFactRevisionValidator.ValidatePredecessor(structuredValidation, predecessor));
    }

    [Theory]
    [InlineData(1, HistoricalEditorialWorkflowState.EditorialReview)]
    [InlineData(2, HistoricalEditorialWorkflowState.StructuredValidation)]
    public void ValidatePredecessor_WhenLegacyMigrationIsRejected_ShouldAcceptRetraction(
        int predecessorRevision,
        HistoricalEditorialWorkflowState predecessorWorkflowState)
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(
            factId,
            predecessorRevision,
            predecessorRevision == 1 ? null : predecessorRevision - 1,
            RecordedAtUtc.AddMinutes(-1),
            predecessorWorkflowState,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);
        HistoricalFact retraction = CreateFact(
            factId,
            predecessorRevision + 1,
            predecessorRevision,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            HistoricalRevisionOrigin.LegacyMigration);

        HistoricalFactRevisionValidator.ValidatePredecessor(retraction, predecessor);
    }

    [Fact]
    public void ValidatePredecessor_WhenLegacyMigrationAdvancesReview_ShouldKeepPendingPublication()
    {
        Guid factId = Guid.NewGuid();
        HistoricalFact predecessor = CreateFact(
            factId,
            1,
            null,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);
        HistoricalFact structuredValidation = CreateFact(
            factId,
            2,
            1,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);

        HistoricalFactRevisionValidator.ValidatePredecessor(structuredValidation, predecessor);
        Assert.Equal(
            HistoricalPublicationState.LegacyPublishedPendingReview,
            structuredValidation.PublicationState);
    }

    private static HistoricalFact CreateFact(
        Guid factId,
        int revision,
        int? supersedesRevision,
        DateTime recordedAtUtc,
        HistoricalEditorialWorkflowState workflowState = HistoricalEditorialWorkflowState.Published,
        HistoricalPublicationState publicationState = HistoricalPublicationState.Published,
        HistoricalRevisionOrigin revisionOrigin = HistoricalRevisionOrigin.Ordinary)
    {
        bool isPublished = publicationState == HistoricalPublicationState.Published;
        bool isLegacyMigration = publicationState
            == HistoricalPublicationState.LegacyPublishedPendingReview;
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));
        return new HistoricalFact(
            factId,
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            period,
            workflowState == HistoricalEditorialWorkflowState.Retracted
                ? HistoricalFactState.Retracted
                : isPublished
                    ? HistoricalFactState.Verified
                    : HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            workflowState,
            publicationState,
            isLegacyMigration
                ? HistoricalLocalizationPolicy.SupportedLanguageCodes
                    .Select(languageCode => new HistoricalLocalizedText(languageCode, "Migration pending review."))
                    .ToArray()
                : Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            new[]
            {
                new HistoricalSourceRevisionReference(
                    Guid.Parse("11111111-1111-1111-1111-111111111111"),
                    1,
                    HistoricalSubjectType.Park,
                    "park-1",
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
                    LifecycleBoundaryMeaning.FirstOperatingDay,
                    null,
                    null),
            },
            null,
            null,
            "history-opening-1998",
            isPublished ? recordedAtUtc.AddMinutes(-2) : null,
            isPublished ? recordedAtUtc.AddMinutes(-1) : null,
            isPublished || isLegacyMigration || publicationState == HistoricalPublicationState.Withdrawn
                ? "hist-v1"
                : null,
            revision,
            supersedesRevision,
            recordedAtUtc,
            revisionOrigin);
    }
}
