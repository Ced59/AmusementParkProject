using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalReviewEventTargetValidatorTests
{
    private static readonly DateTime RecordedAtUtc =
        new DateTime(2026, 9, 26, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ValidateFactTarget_WhenPublishedEventMatchesRevision_ShouldAcceptEvent()
    {
        HistoricalFact fact = CreatePublishedFact();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.Published);

        HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact);
    }

    [Fact]
    public void ValidateFactTarget_WhenTargetDoesNotExist_ShouldRejectEvent()
    {
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            Guid.NewGuid(),
            1,
            HistoricalReviewEventType.Published);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, null));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateSourceTarget_WhenPublishedEventTargetsDraft_ShouldRejectEvent()
    {
        HistoricalSourceReference source = CreateDraftSource();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.Published);

        Assert.Throws<HistoricalPersistenceValidationException>(() =>
            HistoricalReviewEventTargetValidator.ValidateSourceTarget(reviewEvent, source));
    }

    [Fact]
    public void ValidateFactTarget_WhenDraftUpdateTargetsLaterDraftRevision_ShouldAcceptEvent()
    {
        HistoricalFact fact = CreateDraftFact();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.DraftUpdated);

        HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact);
    }

    [Fact]
    public void ValidateSourceTarget_WhenDraftUpdateTargetsLaterDraftRevision_ShouldAcceptEvent()
    {
        HistoricalSourceReference source = CreateDraftSource(revision: 2);
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.DraftUpdated);

        HistoricalReviewEventTargetValidator.ValidateSourceTarget(reviewEvent, source);
    }

    [Fact]
    public void ValidateFactTarget_WhenInitialLegacyRevisionUsesEditorialSubmission_ShouldRejectEvent()
    {
        HistoricalFact fact = CreateLegacyFact();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.SubmittedForEditorialReview);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateSourceTarget_WhenInitialLegacyRevisionUsesEditorialSubmission_ShouldRejectEvent()
    {
        HistoricalSourceReference source = CreateLegacySource();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.SubmittedForEditorialReview);

        HistoricalPersistenceValidationException exception =
            Assert.Throws<HistoricalPersistenceValidationException>(() =>
                HistoricalReviewEventTargetValidator.ValidateSourceTarget(reviewEvent, source));

        Assert.Equal(HistoricalPersistenceErrorCodes.InvalidReviewEvent, exception.ErrorCode);
    }

    [Fact]
    public void ValidateFactTarget_WhenInitialLegacyRevisionUsesMigrated_ShouldAcceptEvent()
    {
        HistoricalFact fact = CreateLegacyFact();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Fact,
            fact.Id,
            fact.Revision,
            HistoricalReviewEventType.Migrated);

        HistoricalReviewEventTargetValidator.ValidateFactTarget(reviewEvent, fact);
    }

    [Fact]
    public void ValidateSourceTarget_WhenInitialLegacyRevisionUsesMigrated_ShouldAcceptEvent()
    {
        HistoricalSourceReference source = CreateLegacySource();
        HistoricalReviewEvent reviewEvent = CreateEvent(
            HistoricalReviewResourceType.Source,
            source.Id,
            source.Revision,
            HistoricalReviewEventType.Migrated);

        HistoricalReviewEventTargetValidator.ValidateSourceTarget(reviewEvent, source);
    }

    private static HistoricalReviewEvent CreateEvent(
        HistoricalReviewResourceType resourceType,
        Guid resourceId,
        int resourceRevision,
        HistoricalReviewEventType eventType)
    {
        return new HistoricalReviewEvent(
            Guid.NewGuid(),
            resourceType,
            resourceId,
            resourceRevision,
            eventType,
            "admin-1",
            null,
            RecordedAtUtc.AddMinutes(1));
    }

    private static HistoricalFact CreatePublishedFact()
    {
        HistoricalPeriod period = HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12));
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
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

    private static HistoricalFact CreateDraftFact()
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForDay(1998, 5, 12)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            Array.Empty<HistoricalLocalizedText>(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "history-opening-1998",
            null,
            null,
            null,
            2,
            1,
            RecordedAtUtc);
    }

    private static HistoricalSourceReference CreateDraftSource(int revision = 1)
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            revision,
            HistoricalSourceType.OfficialWebsite,
            "Page officielle",
            "Parc exemple",
            "https://example.com/history",
            null,
            null,
            new DateOnly(2026, 9, 25),
            "fr",
            null,
            new[] { HistoricalSourceScope.Period },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.Draft,
            HistoricalPublicationState.Draft,
            RecordedAtUtc);
    }

    private static HistoricalFact CreateLegacyFact()
    {
        return new HistoricalFact(
            Guid.NewGuid(),
            new HistoricalSubject(
                HistoricalSubjectType.Park,
                "park-1",
                "Parc exemple",
                HistoricalSubjectPublicationPolicy.FollowCurrentSubject),
            HistoricalFactType.Opening,
            HistoricalPeriod.Point(HistoricalDate.ForYear(1998)),
            HistoricalFactState.Unverified,
            HistoricalImportance.Major,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            HistoricalLocalizationPolicy.SupportedLanguageCodes
                .Select(static languageCode => new HistoricalLocalizedText(
                    languageCode,
                    "Contenu historique hérité en attente de revue."))
                .ToArray(),
            LifecycleBoundaryMeaning.FirstOperatingDay,
            null,
            null,
            null,
            Array.Empty<HistoricalSourceRevisionReference>(),
            null,
            null,
            "legacy-event-1",
            null,
            null,
            "hist-v1-legacy",
            1,
            null,
            RecordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);
    }

    private static HistoricalSourceReference CreateLegacySource()
    {
        return new HistoricalSourceReference(
            Guid.NewGuid(),
            1,
            HistoricalSourceType.OfficialWebsite,
            "Source historique héritée",
            "Éditeur historique",
            "https://example.com/legacy-history",
            null,
            null,
            new DateOnly(2026, 9, 25),
            "fr",
            null,
            new[] { HistoricalSourceScope.Period },
            null,
            HistoricalSourceAccessibility.Accessible,
            HistoricalEditorialWorkflowState.EditorialReview,
            HistoricalPublicationState.LegacyPublishedPendingReview,
            RecordedAtUtc,
            HistoricalRevisionOrigin.LegacyMigration);
    }
}
