using System.Text.Json;
using AmusementPark.Application.Features.AdminAudit.Models;
using AmusementPark.Application.Features.AdminAudit.Ports;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class ShareModerationDecisionJobHandlerTests
{
    private const string TokenValue =
        "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 13, 20, 30, 0, DateTimeKind.Utc);

    [Fact]
    public async Task HandleAsync_WithInvalidPayload_ShouldDeadLetterWithoutReadingState()
    {
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        ShareModerationDecisionJobHandler handler = CreateHandler(reports, jobs);
        DurableBackgroundJobExecutionContext context =
            new DurableBackgroundJobExecutionContext(
                "job-1",
                ShareModerationDecisionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(new { invalid = true }),
                null,
                0,
                null);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            context,
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.DeadLetter, result.Outcome);
        reports.VerifyNoOtherCalls();
        jobs.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_AfterPersistentOutage_ShouldScheduleDurableContinuation()
    {
        ShareModerationDecisionJobPayload payload =
            new ShareModerationDecisionJobPayload(
                "report-1",
                ShareModerationDecision.Suspend,
                "admin-1",
                null,
                NowUtc);
        Mock<IShareModerationReportRepository> reports =
            new Mock<IShareModerationReportRepository>(MockBehavior.Strict);
        reports.Setup(value => value.GetAsync(
                ShareModerationReportId.Parse(payload.ReportId),
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable."));
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == ShareModerationDecisionJob.Kind
                    && request.IdempotencyKey.EndsWith(
                        "continuation:1",
                        StringComparison.Ordinal)),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        ShareModerationDecisionJobHandler handler = CreateHandler(reports, jobs);
        DurableBackgroundJobExecutionContext context =
            new DurableBackgroundJobExecutionContext(
                "job-1",
                ShareModerationDecisionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                null,
                50,
                null);

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            context,
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        reports.VerifyAll();
        jobs.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_AfterDecisionCompletes_ShouldWriteIdempotentWorkerAudit()
    {
        ShareContentPolicy policy = ShareContentPolicy.CreatePrivateDefault(
            SharePublicationType.VisitRecap);
        SharePublication publication = SharePublication.Create(
            SharePublicationId.Parse("publication-audited"),
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
        ShareModerationReport report = ShareModerationReport.Create(
            ShareModerationReportId.Parse("report-audited"),
            ShareModerationTargetType.VisitRecap,
            publication.Id.Value,
            ShareModerationReason.PersonalData,
            null,
            NowUtc.AddMinutes(-1));
        ShareModerationDecisionJobPayload payload =
            new ShareModerationDecisionJobPayload(
                report.Id.Value,
                ShareModerationDecision.Suspend,
                "admin-1",
                "Confirmed.",
                NowUtc);
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
                It.Is<SharePublication>(candidate => candidate.IsModerationSuspended),
                1,
                CancellationToken.None))
            .ReturnsAsync(SharePublicationWriteOutcome.Success);
        Mock<IDurableBackgroundJobRepository> jobs =
            new Mock<IDurableBackgroundJobRepository>(MockBehavior.Strict);
        jobs.Setup(value => value.EnqueueExactAsync(
                It.Is<EnqueueExactBackgroundJobRequest>(request =>
                    request.Kind == SharePublicationCacheInvalidationJob.Kind),
                CancellationToken.None))
            .ReturnsAsync((DurableBackgroundJob)null!);
        Mock<IAdminAuditLogWriter> auditLogWriter =
            new Mock<IAdminAuditLogWriter>(MockBehavior.Strict);
        auditLogWriter.Setup(value => value.WriteAsync(
                It.Is<AdminAuditLogEntry>(entry =>
                    entry.Id == "share-moderation-decision:job-audited"
                    && entry.Action == "share-moderation.report.decision-completed"
                    && entry.EntityId == report.Id.Value
                    && entry.ActorUserId == "admin-1"
                    && entry.Metadata["decision"] == ShareModerationDecision.Suspend.ToString()),
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        ShareModerationDecisionJobHandler handler = CreateHandler(
            reports,
            jobs,
            auditLogWriter,
            publications);
        DurableBackgroundJobExecutionContext context =
            new DurableBackgroundJobExecutionContext(
                "job-audited",
                ShareModerationDecisionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                null,
                0,
                "trace-audited");

        DurableBackgroundJobHandlerResult result = await handler.HandleAsync(
            context,
            CancellationToken.None);

        Assert.Equal(DurableBackgroundJobHandlerOutcome.Succeeded, result.Outcome);
        reports.VerifyAll();
        publications.VerifyAll();
        jobs.VerifyAll();
        auditLogWriter.VerifyAll();
    }

    private static ShareModerationDecisionJobHandler CreateHandler(
        Mock<IShareModerationReportRepository> reports,
        Mock<IDurableBackgroundJobRepository> jobs,
        Mock<IAdminAuditLogWriter>? auditLogWriter = null,
        Mock<ISharePublicationRepository>? publications = null)
    {
        Mock<ISharePublicationRepository> publicationRepository = publications
            ?? new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        Mock<IAdminAuditLogWriter> workerAudit = auditLogWriter
            ?? new Mock<IAdminAuditLogWriter>(MockBehavior.Strict);
        ShareModerationDecisionScheduler scheduler =
            new ShareModerationDecisionScheduler(jobs.Object);
        ShareModerationDecisionExecutor executor =
            new ShareModerationDecisionExecutor(
                reports.Object,
                new ShareModerationPublicationTargetExecutor(
                    publicationRepository.Object,
                    new SharePublicationCacheInvalidationScheduler(jobs.Object)),
                new ShareModerationComparisonTargetExecutor(comparisons.Object));
        return new ShareModerationDecisionJobHandler(
            executor,
            scheduler,
            workerAudit.Object);
    }
}
