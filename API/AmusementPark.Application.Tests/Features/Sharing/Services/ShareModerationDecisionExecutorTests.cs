using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ShareModerationDecisionExecutorTests
{
    private const string TokenValue =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private const string ReplacementTokenValue =
        "ISIjJCUmJygpKissLS4vMDEyMzQ1Njc4OTo7PD0-P0A";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ExecuteAsync_AfterReportWriteConflict_ShouldFinishOnRetryWithoutResuspending()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport firstRead = CreatePendingReport(publication);
        ShareModerationReport retryRead = CreatePendingReport(publication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.SetupSequence(value => value.GetAsync(firstRead.Id, CancellationToken.None))
            .ReturnsAsync(firstRead)
            .ReturnsAsync(retryRead);
        reports.SetupSequence(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Conflict)
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.HasModerationSuspension(firstRead.Id)),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs = CreateCacheJobRepository();
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);
        ShareModerationDecisionJobPayload payload = CreatePayload(firstRead.Id);

        ShareModerationDecisionExecutionOutcome firstOutcome =
            await executor.ExecuteAsync(payload, CancellationToken.None);
        ShareModerationDecisionExecutionOutcome retryOutcome =
            await executor.ExecuteAsync(payload, CancellationToken.None);

        Assert.Equal(
            ShareModerationDecisionExecutionOutcome.RetryableConflict,
            firstOutcome);
        Assert.Equal(ShareModerationDecisionExecutionOutcome.Succeeded, retryOutcome);
        Assert.True(publication.HasModerationSuspension(firstRead.Id));
        publications.Verify(value => value.ReplaceAsync(
            It.IsAny<SharePublication>(),
            It.IsAny<long>(),
            CancellationToken.None), Times.Once);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task ExecuteAsync_WhenReportedPublicationWasReplaced_ShouldSuspendTheReplacement(
        bool replacementWasPublished)
    {
        SharePublication reportedPublication = CreatePublishedPublication();
        ShareModerationReport report = CreatePendingReport(reportedPublication);
        reportedPublication.Revoke(1, NowUtc.AddMinutes(-4));
        SharePublication replacement = SharePublication.Create(
            SharePublicationId.Parse("publication-2"),
            reportedPublication.OwnerUserId,
            reportedPublication.Type,
            reportedPublication.SourceScopeKey,
            reportedPublication.ContentPolicy,
            reportedPublication.SourceVersion,
            NowUtc.AddMinutes(-3));
        if (replacementWasPublished)
        {
            replacement.Publish(
                ShareToken.Parse(ReplacementTokenValue),
                ShareVisibility.Unlisted,
                replacement.SourceVersion,
                replacement.ContentPolicy,
                0,
                NowUtc.AddMinutes(-2));
        }

        long replacementVersion = replacement.Version;
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(
                reportedPublication.Id,
                CancellationToken.None))
            .ReturnsAsync(reportedPublication);
        publications.Setup(value => value.GetOwnedBySourceAsync(
                reportedPublication.OwnerUserId,
                reportedPublication.Type,
                reportedPublication.SourceScopeKey,
                CancellationToken.None))
            .ReturnsAsync(replacement);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Id == replacement.Id
                    && candidate.HasModerationSuspension(report.Id)),
                replacementVersion,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs = replacementWasPublished
            ? CreateCacheJobRepository()
            : new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);

        ShareModerationDecisionExecutionOutcome outcome = await executor.ExecuteAsync(
            CreatePayload(report.Id),
            CancellationToken.None);

        Assert.Equal(ShareModerationDecisionExecutionOutcome.Succeeded, outcome);
        Assert.False(reportedPublication.IsModerationSuspended);
        Assert.True(replacement.HasModerationSuspension(report.Id));
        Assert.False(replacement.IsResolvable);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReplacementWinsTheSourceLock_ShouldRetryBeforeAuditingSuspension()
    {
        SharePublication firstReportedPublication = CreatePublishedPublication();
        firstReportedPublication.Revoke(1, NowUtc.AddMinutes(-4));
        SharePublication retryReportedPublication = SharePublication.Restore(
            firstReportedPublication.Id,
            firstReportedPublication.OwnerUserId,
            firstReportedPublication.Type,
            firstReportedPublication.SourceScopeKey,
            null,
            firstReportedPublication.Status,
            firstReportedPublication.Visibility,
            firstReportedPublication.ContentPolicy,
            firstReportedPublication.SourceVersion,
            firstReportedPublication.PublicationVersion,
            firstReportedPublication.Version,
            firstReportedPublication.PublishedAtUtc,
            firstReportedPublication.RevokedAtUtc,
            firstReportedPublication.CreatedAtUtc,
            firstReportedPublication.UpdatedAtUtc,
            firstReportedPublication.ContentFingerprint);
        SharePublication replacement = SharePublication.Create(
            SharePublicationId.Parse("publication-race-winner"),
            firstReportedPublication.OwnerUserId,
            firstReportedPublication.Type,
            firstReportedPublication.SourceScopeKey,
            firstReportedPublication.ContentPolicy,
            firstReportedPublication.SourceVersion,
            NowUtc.AddMinutes(-3));
        replacement.Publish(
            ShareToken.Parse(ReplacementTokenValue),
            ShareVisibility.Unlisted,
            replacement.SourceVersion,
            replacement.ContentPolicy,
            0,
            NowUtc.AddMinutes(-2));
        ShareModerationReport report = CreatePendingReport(firstReportedPublication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.SetupSequence(value => value.GetByIdAsync(
                firstReportedPublication.Id,
                CancellationToken.None))
            .ReturnsAsync(firstReportedPublication)
            .ReturnsAsync(retryReportedPublication);
        publications.SetupSequence(value => value.GetOwnedBySourceAsync(
                firstReportedPublication.OwnerUserId,
                firstReportedPublication.Type,
                firstReportedPublication.SourceScopeKey,
                CancellationToken.None))
            .ReturnsAsync(firstReportedPublication)
            .ReturnsAsync(replacement);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Id == firstReportedPublication.Id),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Conflict);
        long replacementVersion = replacement.Version;
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.Id == replacement.Id
                    && candidate.HasModerationSuspension(report.Id)),
                replacementVersion,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs = CreateCacheJobRepository();
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);

        ShareModerationDecisionExecutionOutcome firstOutcome = await executor.ExecuteAsync(
            CreatePayload(report.Id),
            CancellationToken.None);
        ShareModerationDecisionExecutionOutcome retryOutcome = await executor.ExecuteAsync(
            CreatePayload(report.Id),
            CancellationToken.None);

        Assert.Equal(
            ShareModerationDecisionExecutionOutcome.RetryableConflict,
            firstOutcome);
        Assert.Equal(ShareModerationDecisionExecutionOutcome.Succeeded, retryOutcome);
        Assert.True(replacement.HasModerationSuspension(report.Id));
        Assert.False(replacement.IsResolvable);
        Assert.Equal(ShareModerationReportStatus.PublicationSuspended, report.Status);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_RestoreFromOlderReport_ShouldNotClearNewerSuspension()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport olderReport = CreatePendingReport(publication);
        olderReport.MarkPublicationSuspended("admin-1", null, NowUtc.AddMinutes(-2));
        ShareModerationReportId newerReportId =
            ShareModerationReportId.Parse("report-newer");
        publication.SuspendByModeration(newerReportId, NowUtc.AddMinutes(-1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(olderReport.Id, CancellationToken.None))
            .ReturnsAsync(olderReport);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationRestored),
                1,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IDurableBackgroundJobRepository> jobs = CreateCacheJobRepository();
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);
        ShareModerationDecisionJobPayload payload = new ShareModerationDecisionJobPayload(
            olderReport.Id.Value,
            ShareModerationDecision.Restore,
            "admin-2",
            null,
            NowUtc);

        ShareModerationDecisionExecutionOutcome outcome =
            await executor.ExecuteAsync(payload, CancellationToken.None);

        Assert.Equal(ShareModerationDecisionExecutionOutcome.Succeeded, outcome);
        Assert.True(publication.HasModerationSuspension(newerReportId));
        publications.Verify(value => value.ReplaceAsync(
            It.IsAny<SharePublication>(),
            It.IsAny<long>(),
            CancellationToken.None), Times.Never);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_CompetingSuspension_ShouldPreventGapDuringOlderRestore()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReportId olderReportId =
            ShareModerationReportId.Parse("report-older");
        publication.SuspendByModeration(olderReportId, NowUtc.AddMinutes(-2));
        ShareModerationReport pendingReport = CreatePendingReport(publication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(pendingReport.Id, CancellationToken.None))
            .ReturnsAsync(pendingReport);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.HasModerationSuspension(olderReportId)
                    && candidate.HasModerationSuspension(pendingReport.Id)),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs = CreateCacheJobRepository();
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);
        ShareModerationDecisionJobPayload payload = CreatePayload(pendingReport.Id);

        ShareModerationDecisionExecutionOutcome suspensionOutcome =
            await executor.ExecuteAsync(payload, CancellationToken.None);
        publication.RestoreAfterModeration(olderReportId, NowUtc);

        Assert.Equal(
            ShareModerationDecisionExecutionOutcome.Succeeded,
            suspensionOutcome);
        Assert.False(publication.HasModerationSuspension(olderReportId));
        Assert.True(publication.HasModerationSuspension(pendingReport.Id));
        Assert.False(publication.IsResolvable);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ComparisonTarget_CompetingSuspension_ShouldPreventRestorationGap()
    {
        ProfileComparison comparison = CreateComparison();
        ShareModerationReportId olderReportId =
            ShareModerationReportId.Parse("report-older");
        comparison.SuspendByModeration(olderReportId, NowUtc.AddMinutes(-2));
        ShareModerationReport pendingReport = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-newer"),
            ShareModerationTargetType.ProfileComparison,
            comparison.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-1));
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        comparisons.Setup(value => value.GetByIdAsync(
                comparison.Id,
                CancellationToken.None))
            .ReturnsAsync(comparison);
        comparisons.Setup(value => value.ReplaceAsync(
                It.Is<ProfileComparison>(candidate =>
                    candidate.HasModerationSuspension(olderReportId)
                    && candidate.HasModerationSuspension(pendingReport.Id)),
                1,
                CancellationToken.None))
            .ReturnsAsync(ProfileComparisonWriteOutcome.Success);
        ShareModerationComparisonTargetExecutor executor =
            new ShareModerationComparisonTargetExecutor(
                comparisons.Object,
                new SharePublicationFixedTimeProvider(NowUtc));

        ShareModerationDecisionExecutionOutcome outcome = await executor.SuspendAsync(
            pendingReport,
            CancellationToken.None);

        Assert.Equal(
            ShareModerationDecisionExecutionOutcome.Succeeded,
            outcome);
        comparison.RestoreAfterModeration(olderReportId, NowUtc);
        Assert.False(comparison.HasModerationSuspension(olderReportId));
        Assert.True(comparison.HasModerationSuspension(pendingReport.Id));
        Assert.False(comparison.IsPubliclyResolvable);
        comparisons.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_WhenReportWasDismissedAfterSuspension_ShouldCompensateTarget()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport report = CreatePendingReport(publication);
        publication.SuspendByModeration(report.Id, NowUtc.AddMinutes(-2));
        report.Dismiss("admin-2", "Dismissed after review.", NowUtc.AddMinutes(-1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate => !candidate.IsModerationSuspended),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs = CreateCacheJobRepository();
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);

        ShareModerationDecisionExecutionOutcome outcome = await executor.ExecuteAsync(
            CreatePayload(report.Id),
            CancellationToken.None);

        Assert.Equal(
            ShareModerationDecisionExecutionOutcome.InvalidTransition,
            outcome);
        Assert.True(publication.IsResolvable);
        Assert.Empty(publication.ModerationSuspensionReportIds);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ExecuteAsync_AfterInvalidationSchedulingFailure_ShouldScheduleAgainOnReplay()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport firstRead = CreatePendingReport(publication);
        ShareModerationReport retryRead = CreatePendingReport(publication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.SetupSequence(value => value.GetAsync(firstRead.Id, CancellationToken.None))
            .ReturnsAsync(firstRead)
            .ReturnsAsync(retryRead);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.HasModerationSuspension(firstRead.Id)),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.SetupSequence(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == SharePublicationCacheInvalidationJob.Kind),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Job storage unavailable."))
            .ReturnsAsync((DurableBackgroundJob)null!);
        ShareModerationDecisionExecutor executor = CreateExecutor(
            reports,
            publications,
            jobs);
        ShareModerationDecisionJobPayload payload = CreatePayload(firstRead.Id);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            executor.ExecuteAsync(payload, CancellationToken.None));
        ShareModerationDecisionExecutionOutcome retryOutcome =
            await executor.ExecuteAsync(payload, CancellationToken.None);

        Assert.Equal(ShareModerationDecisionExecutionOutcome.Succeeded, retryOutcome);
        Assert.True(publication.HasModerationSuspension(firstRead.Id));
        jobs.Verify(value => value.EnqueueExactAsync(
            It.IsAny<EnqueueExactBackgroundJobRequest>(),
            CancellationToken.None), Times.Exactly(2));
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    private static ShareModerationDecisionExecutor CreateExecutor(
        Mock<IShareModerationReportRepository> reports,
        Mock<ISharePublicationRepository> publications,
        Mock<IDurableBackgroundJobRepository> jobs)
    {
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        TimeProvider timeProvider = new SharePublicationFixedTimeProvider(NowUtc);
        return new ShareModerationDecisionExecutor(
            reports.Object,
            new ShareModerationPublicationTargetExecutor(
                publications.Object,
                new SharePublicationCacheInvalidationScheduler(jobs.Object),
                timeProvider),
            new ShareModerationComparisonTargetExecutor(
                comparisons.Object,
                timeProvider));
    }

    private static Mock<IDurableBackgroundJobRepository> CreateCacheJobRepository()
    {
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == SharePublicationCacheInvalidationJob.Kind),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        return jobs;
    }

    private static ShareModerationDecisionJobPayload CreatePayload(
        ShareModerationReportId reportId)
    {
        return new ShareModerationDecisionJobPayload(
            reportId.Value,
            ShareModerationDecision.Suspend,
            "admin-1",
            "Confirmed.",
            NowUtc);
    }

    private static ShareModerationReport CreatePendingReport(
        SharePublication publication)
    {
        return ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-1"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-5));
    }

    private static SharePublication CreatePublishedPublication()
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(
            SharePublicationType.VisitRecap);
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            "owner-1",
            SharePublicationType.VisitRecap,
            "visit-1",
            policy,
            1,
            NowUtc.AddMinutes(-10));
        publication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            1,
            policy,
            0,
            NowUtc.AddMinutes(-9));
        return publication;
    }

    private static ProfileComparison CreateComparison()
    {
        ProfileComparisonCalculation calculation = new ProfileComparisonCalculation(
            "Camille",
            "Alex",
            new[] { ProfileComparisonCategory.VisitedParks },
            Array.Empty<ProfileComparisonParkResult>(),
            Array.Empty<ProfileComparisonRatingResult>(),
            Array.Empty<ProfileComparisonYearResult>(),
            Array.Empty<ProfileComparisonMissedItemResult>(),
            0,
            ProfileComparisonCalculator.MinimumRatingsForCorrelation,
            null,
            false,
            ProfileComparisonCalculator.CalculationVersion);
        return ProfileComparison.Create(
            ProfileComparisonId.Parse("comparison-1"),
            ProfileComparisonInvitationId.Parse("invitation-1"),
            ShareToken.Parse(TokenValue),
            "creator-1",
            "acceptor-1",
            SharePublicationId.Parse("creator-passport"),
            1,
            SharePublicationId.Parse("acceptor-passport"),
            1,
            calculation,
            NowUtc.AddMinutes(-10));
    }
}
