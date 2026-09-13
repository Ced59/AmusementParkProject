using System.Text.Json;
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

    private static ShareModerationDecisionJobHandler CreateHandler(
        Mock<IShareModerationReportRepository> reports,
        Mock<IDurableBackgroundJobRepository> jobs)
    {
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        Mock<IProfileComparisonRepository> comparisons =
            new Mock<IProfileComparisonRepository>(MockBehavior.Strict);
        ShareModerationDecisionScheduler scheduler =
            new ShareModerationDecisionScheduler(jobs.Object);
        ShareModerationDecisionExecutor executor =
            new ShareModerationDecisionExecutor(
                reports.Object,
                new ShareModerationPublicationTargetExecutor(
                    publications.Object,
                    new SharePublicationCacheInvalidationScheduler(jobs.Object)),
                new ShareModerationComparisonTargetExecutor(comparisons.Object));
        return new ShareModerationDecisionJobHandler(executor, scheduler);
    }
}
