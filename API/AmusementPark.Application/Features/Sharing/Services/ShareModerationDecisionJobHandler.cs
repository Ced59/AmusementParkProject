using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationDecisionJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumAttempts = 100;
    private const int ContinuationAttemptThreshold = MaximumAttempts / 2;
    private readonly ShareModerationDecisionExecutor executor;
    private readonly ShareModerationDecisionScheduler scheduler;

    public ShareModerationDecisionJobHandler(
        ShareModerationDecisionExecutor executor,
        ShareModerationDecisionScheduler scheduler)
    {
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            ShareModerationDecisionJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { ShareModerationDecisionJob.PayloadVersion },
            TimeSpan.FromMinutes(1),
            maximumAttempts: MaximumAttempts,
            initialRetryDelay: TimeSpan.FromSeconds(5),
            maximumRetryDelay: TimeSpan.FromMinutes(5),
            maximumConcurrency: 1);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        ShareModerationDecisionJobPayload? payload = Deserialize(context);
        if (payload is null
            || !ShareModerationReportId.TryParse(payload.ReportId, out _)
            || payload.Decision is not ShareModerationDecision.Suspend
                and not ShareModerationDecision.Restore
            || string.IsNullOrWhiteSpace(payload.ReviewerUserId)
            || payload.RequestedAtUtc.Kind != DateTimeKind.Utc
            || payload.Continuation < 0)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                "share-moderation.invalid-decision-payload");
        }

        try
        {
            ShareModerationDecisionExecutionOutcome outcome =
                await this.executor.ExecuteAsync(payload, cancellationToken);
            return outcome switch
            {
                ShareModerationDecisionExecutionOutcome.Succeeded =>
                    DurableBackgroundJobHandlerResult.Success(),
                ShareModerationDecisionExecutionOutcome.RetryableConflict =>
                    await this.RetryOrContinueAsync(
                        payload,
                        context.AttemptCount,
                        "share-moderation.concurrent-decision",
                        cancellationToken),
                ShareModerationDecisionExecutionOutcome.ReportNotFound =>
                    DurableBackgroundJobHandlerResult.DeadLetter(
                        "share-moderation.report-not-found"),
                ShareModerationDecisionExecutionOutcome.TargetNotFound =>
                    DurableBackgroundJobHandlerResult.DeadLetter(
                        "share-moderation.target-not-found"),
                _ => DurableBackgroundJobHandlerResult.DeadLetter(
                    "share-moderation.invalid-transition"),
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return await this.RetryOrContinueAsync(
                payload,
                context.AttemptCount,
                "share-moderation.dependency-unavailable",
                cancellationToken);
        }
    }

    private async Task<DurableBackgroundJobHandlerResult> RetryOrContinueAsync(
        ShareModerationDecisionJobPayload payload,
        int attemptCount,
        string errorCode,
        CancellationToken cancellationToken)
    {
        if (attemptCount < ContinuationAttemptThreshold)
        {
            return DurableBackgroundJobHandlerResult.Retry(errorCode);
        }

        await this.scheduler.ScheduleContinuationAsync(payload, cancellationToken);
        return DurableBackgroundJobHandlerResult.Success();
    }

    private static ShareModerationDecisionJobPayload? Deserialize(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != ShareModerationDecisionJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<ShareModerationDecisionJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
