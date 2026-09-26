using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalSourceRevisionValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidatePredecessor_WhenImmediatelyPriorRevisionExists_ShouldAcceptCorrection()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(sourceId, 5, RecordedAtUtc.AddMinutes(-1));
        HistoricalSourceReference correction = CreateSource(
            sourceId,
            6,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Corrected,
            HistoricalPublicationState.Published);

        HistoricalSourceRevisionValidator.ValidatePredecessor(correction, predecessor);
    }

    [Fact]
    public void ValidatePredecessor_WhenPriorRevisionIsMissing_ShouldRejectCorrection()
    {
        HistoricalSourceReference correction = CreateSource(Guid.NewGuid(), 2, RecordedAtUtc);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalSourceRevisionValidator.ValidatePredecessor(correction, null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidRevision, exception.ErrorCode);
    }

    [Fact]
    public void ValidatePredecessor_WhenPriorRevisionBelongsToAnotherSource_ShouldRejectCorrection()
    {
        HistoricalSourceReference predecessor = CreateSource(Guid.NewGuid(), 5, RecordedAtUtc.AddMinutes(-1));
        HistoricalSourceReference correction = CreateSource(Guid.NewGuid(), 6, RecordedAtUtc);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalSourceRevisionValidator.ValidatePredecessor(correction, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenCorrectionFollowsDraft_ShouldRejectTransition()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(
            sourceId,
            1,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);
        HistoricalSourceReference correction = CreateSource(
            sourceId,
            2,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Corrected,
            HistoricalPublicationState.Published);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalSourceRevisionValidator.ValidatePredecessor(correction, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenPublishedSourceReturnsToDraft_ShouldRejectTransition()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(sourceId, 5, RecordedAtUtc.AddMinutes(-1));
        HistoricalSourceReference draft = CreateSource(
            sourceId,
            6,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalSourceRevisionValidator.ValidatePredecessor(draft, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenSourceSkipsWorkflowStage_ShouldRejectTransition()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(
            sourceId,
            1,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);
        HistoricalSourceReference structuredValidation = CreateSource(
            sourceId,
            2,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.Draft);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalSourceRevisionValidator.ValidatePredecessor(structuredValidation, predecessor));
    }

    [Fact]
    public void ValidatePredecessor_WhenSourceMovesFromDraftToEditorialReview_ShouldAcceptTransition()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(
            sourceId,
            1,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft);
        HistoricalSourceReference editorialReview = CreateSource(
            sourceId,
            2,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.Draft);

        HistoricalSourceRevisionValidator.ValidatePredecessor(editorialReview, predecessor);
    }

    [Fact]
    public void ValidatePredecessor_WhenLegacyMigrationIsRejected_ShouldAcceptRetraction()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(
            sourceId,
            1,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);
        HistoricalSourceReference retraction = CreateSource(
            sourceId,
            2,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.Retracted,
            HistoricalPublicationState.Withdrawn,
            HistoricalRevisionOrigin.LegacyMigration);

        HistoricalSourceRevisionValidator.ValidatePredecessor(retraction, predecessor);
    }

    [Fact]
    public void ValidatePredecessor_WhenLegacyMigrationAdvancesReview_ShouldKeepPendingPublication()
    {
        Guid sourceId = Guid.NewGuid();
        HistoricalSourceReference predecessor = CreateSource(
            sourceId,
            1,
            RecordedAtUtc.AddMinutes(-1),
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);
        HistoricalSourceReference structuredValidation = CreateSource(
            sourceId,
            2,
            RecordedAtUtc,
            HistoricalEditorialWorkflowState.StructuredValidation,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalRevisionOrigin.LegacyMigration);

        HistoricalSourceRevisionValidator.ValidatePredecessor(structuredValidation, predecessor);
        Assert.Equal(
            HistoricalPublicationState.LegacyPublishedPendingReview,
            structuredValidation.PublicationState);
    }

    private static HistoricalSourceReference CreateSource(
        Guid sourceId,
        int revision,
        DateTime recordedAtUtc,
        HistoricalEditorialWorkflowState workflowState = HistoricalEditorialWorkflowState.Published,
        HistoricalPublicationState publicationState = HistoricalPublicationState.Published,
        HistoricalRevisionOrigin revisionOrigin = HistoricalRevisionOrigin.Ordinary)
    {
        return new HistoricalSourceReference(
            sourceId,
            revision,
            HistoricalSourceType.OfficialWebsite,
            "Page officielle",
            "Parc exemple",
            "https://example.com/history",
            null,
            new DateOnly(1998, 5, 12),
            new DateOnly(2026, 9, 25),
            "fr",
            null,
            new[] { HistoricalSourceScope.Period },
            null,
            HistoricalSourceAccessibility.Accessible,
            workflowState,
            publicationState,
            recordedAtUtc,
            revisionOrigin);
    }
}
