using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Tests.Features.Sharing.Handlers;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ShareModerationServiceTests
{
    private const string TokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 19, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task SubmitAsync_ForResolvablePublication_ShouldPersistOnlyItsInternalTargetReference()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.CreateAsync(
                It.Is<ShareModerationReport>(report =>
                    report.TargetRecordId == publication.Id.Value
                    && report.TargetType == ShareModerationTargetType.VisitRecap
                    && report.Reason == ShareModerationReason.PersonalData
                    && report.Details == "A phone number is visible."),
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        ShareModerationService service = CreateService(reports, publications);

        ApplicationResult result = await service.SubmitAsync(
            new SubmitShareModerationReportCommand(
                ShareModerationTargetType.VisitRecap,
                TokenValue,
                ShareModerationReason.PersonalData,
                "A phone number is visible."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        reports.VerifyAll();
        publications.VerifyAll();
    }

    [Fact]
    public async Task SubmitAsync_WithUnsafeLink_ShouldRejectWithoutPersisting()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        ShareModerationService service = CreateService(reports, publications);

        ApplicationResult result = await service.SubmitAsync(
            new SubmitShareModerationReportCommand(
                ShareModerationTargetType.VisitRecap,
                TokenValue,
                ShareModerationReason.SpamOrUnsafeLink,
                "See https://unsafe.example"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-moderation.report-invalid");
        reports.VerifyNoOtherCalls();
        publications.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_SuspendPublication_ShouldCutPublicResolutionAndAuditReport()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-1"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationSuspended
                    && candidate.ReviewedByUserId == "admin-1"),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.HasModerationSuspension(report.Id)
                    && !candidate.IsResolvable),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        List<EnqueueExactBackgroundJobRequest> enqueuedJobs =
            new List<EnqueueExactBackgroundJobRequest>();
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .Callback<EnqueueExactBackgroundJobRequest, CancellationToken>(
                (request, _) => enqueuedJobs.Add(request))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-1",
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "Confirmed."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(1, publication.PublicationVersion);
        Assert.Equal(
            new[]
            {
                ShareModerationDecisionJob.Kind,
                SharePublicationCacheInvalidationJob.Kind,
            },
            enqueuedJobs.Select(static request => request.Kind));
        jobs.VerifyAll();
        publications.VerifyAll();
        reports.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_WhenImmediateExecutionFailsAfterScheduling_ShouldAcknowledgeAuditableDecision()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-queued"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("MongoDB unavailable"));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == ShareModerationDecisionJob.Kind),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-1",
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "Confirmed."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        jobs.VerifyAll();
        publications.VerifyAll();
        reports.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_WhenImmediateExecutionConflictsAfterScheduling_ShouldAcknowledgeAuditableDecision()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-conflict"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.IsAny<SharePublication>(),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Conflict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == ShareModerationDecisionJob.Kind),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-1",
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "Confirmed."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        jobs.VerifyAll();
        publications.VerifyAll();
        reports.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_WhenAnotherReportCurrentlySuspendsTarget_ShouldKeepQueuedDecisionAuditable()
    {
        SharePublication publication = CreatePublishedPublication();
        publication.SuspendByModeration(
            ShareModerationReportId.Parse("report-active"),
            NowUtc.AddSeconds(-30));
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-queued-after-active"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Another disclosure is visible.",
            NowUtc.AddMinutes(-1));
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
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    candidate.HasModerationSuspension(
                        ShareModerationReportId.Parse("report-active"))
                    && candidate.HasModerationSuspension(report.Id)),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-2",
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "Confirmed separately."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(publication.HasModerationSuspension(
            ShareModerationReportId.Parse("report-active")));
        Assert.True(publication.HasModerationSuspension(report.Id));
        Assert.False(publication.IsResolvable);
        jobs.VerifyAll();
        publications.VerifyAll();
        reports.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_DuplicateDecision_ShouldExecuteThePersistedAuditPayload()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-canonical"),
            ShareModerationTargetType.VisitRecap,
            "publication-1",
            ShareModerationReason.Other,
            "Needs a decision.",
            NowUtc.AddMinutes(-1));
        ShareModerationDecisionJobPayload persistedPayload =
            new ShareModerationDecisionJobPayload(
                report.Id.Value,
                ShareModerationDecision.Dismiss,
                "admin-original",
                "Original decision.",
                NowUtc.AddSeconds(-10));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.Dismissed
                    && candidate.ReviewedByUserId == "admin-original"
                    && candidate.DecisionNote == "Original decision."
                    && candidate.ReviewedAtUtc == persistedPayload.RequestedAtUtc),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request, persistedPayload));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-duplicate",
                report.Id.Value,
                ShareModerationDecision.Dismiss,
                "Different decision."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        reports.VerifyAll();
        publications.VerifyNoOtherCalls();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_ContradictoryDecisionOnSameReportVersion_ShouldRejectSecondJob()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-serialized"),
            ShareModerationTargetType.VisitRecap,
            "publication-1",
            ShareModerationReason.PersonalData,
            "Needs a decision.",
            NowUtc.AddMinutes(-1));
        ShareModerationDecisionJobPayload acceptedPayload =
            new ShareModerationDecisionJobPayload(
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "admin-original",
                "Confirmed disclosure.",
                NowUtc.AddSeconds(-10),
                report.Version);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == ShareModerationDecisionJob.Kind
                    && request.IdempotencyKey ==
                        "share-moderation:report-serialized:report-version:0:continuation:0"),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request, acceptedPayload));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-second",
                report.Id.Value,
                ShareModerationDecision.Dismiss,
                "Dismiss instead."),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-moderation.concurrent-modification");
        reports.VerifyAll();
        publications.VerifyNoOtherCalls();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_WhenClockMovedBack_ShouldClampDecisionToSubmissionTime()
    {
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-clock"),
            ShareModerationTargetType.VisitRecap,
            "publication-1",
            ShareModerationReason.Other,
            "Needs a decision.",
            NowUtc.AddMinutes(1));
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.Dismissed
                    && candidate.ReviewedAtUtc == report.SubmittedAtUtc),
                0,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-1",
                report.Id.Value,
                ShareModerationDecision.Dismiss,
                null),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(report.SubmittedAtUtc, report.ReviewedAtUtc);
        reports.VerifyAll();
        publications.VerifyNoOtherCalls();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task ReviewAsync_WhenClockMovedBackAfterSuspension_ShouldKeepRestorationChronological()
    {
        SharePublication publication = CreatePublishedPublication();
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-restore-clock"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            "Personal data is visible.",
            NowUtc.AddMinutes(-1));
        DateTime suspendedAtUtc = NowUtc.AddMinutes(1);
        report.MarkPublicationSuspended("admin-1", "Confirmed.", suspendedAtUtc);
        publication.SuspendByModeration(report.Id, suspendedAtUtc);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(report.Id, CancellationToken.None))
            .ReturnsAsync(report);
        reports.Setup(value => value.ReplaceAsync(
                It.Is<ShareModerationReport>(candidate =>
                    candidate.Status == ShareModerationReportStatus.PublicationRestored
                    && candidate.ReviewedAtUtc == suspendedAtUtc),
                1,
                CancellationToken.None))
            .ReturnsAsync(ShareModerationReportWriteOutcome.Success);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetByIdAsync(publication.Id, CancellationToken.None))
            .ReturnsAsync(publication);
        publications.Setup(value => value.ReplaceAsync(
                It.Is<SharePublication>(candidate =>
                    !candidate.HasModerationSuspension(report.Id)),
                2,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.IsAny<EnqueueExactBackgroundJobRequest>(),
                CancellationToken.None))
            .ReturnsAsync((EnqueueExactBackgroundJobRequest request, CancellationToken _) =>
                CreateQueuedJob(request));
        ShareModerationService service = CreateService(reports, publications, jobs);

        ApplicationResult result = await service.ReviewAsync(
            new ReviewShareModerationReportCommand(
                "admin-2",
                report.Id.Value,
                ShareModerationDecision.Restore,
                "Restored."),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(suspendedAtUtc, report.ReviewedAtUtc);
        Assert.False(publication.IsModerationSuspended);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
    }

    private static ShareModerationService CreateService(
        Mock<IShareModerationReportRepository> reports,
        Mock<ISharePublicationRepository> publications,
        Mock<IDurableBackgroundJobRepository>? jobs = null)
    {
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobRepository = jobs
            ?? new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        SharePublicationCacheInvalidationScheduler invalidationScheduler =
            new SharePublicationCacheInvalidationScheduler(jobRepository.Object);
        ShareModerationDecisionScheduler decisionScheduler =
            new ShareModerationDecisionScheduler(jobRepository.Object);
        TimeProvider timeProvider = new SharePublicationFixedTimeProvider(NowUtc);
        ShareModerationDecisionExecutor decisionExecutor =
            new ShareModerationDecisionExecutor(
                reports.Object,
                new ShareModerationPublicationTargetExecutor(
                    publications.Object,
                    invalidationScheduler,
                    timeProvider),
                new ShareModerationComparisonTargetExecutor(
                    comparisons.Object,
                    timeProvider));
        return new ShareModerationService(
            reports.Object,
            publications.Object,
            comparisons.Object,
            decisionScheduler,
            decisionExecutor,
            timeProvider);
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
            NowUtc.AddMinutes(-5));
        publication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            1,
            policy,
            0,
            NowUtc.AddMinutes(-4));
        return publication;
    }

    private static DurableBackgroundJob CreateQueuedJob(
        EnqueueExactBackgroundJobRequest request,
        ShareModerationDecisionJobPayload? persistedPayload = null)
    {
        DateTime nowUtc = NowUtc.AddSeconds(-10);
        return new DurableBackgroundJob(
            "job-1",
            request.Kind,
            null,
            request.IdempotencyKey,
            request.PayloadVersion,
            persistedPayload is null
                ? request.Payload
                : System.Text.Json.JsonSerializer.SerializeToElement(persistedPayload),
            null,
            null,
            DurableBackgroundJobStatus.Pending,
            request.Priority,
            0,
            nowUtc,
            null,
            null,
            null,
            nowUtc,
            nowUtc,
            null,
            null,
            null);
    }
}
