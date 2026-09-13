using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class ShareModerationDecisionScheduler
{
    private readonly IDurableBackgroundJobRepository jobRepository;

    public ShareModerationDecisionScheduler(IDurableBackgroundJobRepository jobRepository)
    {
        this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
    }

    public async Task<ShareModerationDecisionJobPayload> ScheduleAsync(
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        DurableBackgroundJob job = await this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                ShareModerationDecisionJob.Kind,
                $"share-moderation:{payload.ReportId}:{payload.Decision}:continuation:{payload.Continuation}",
                ShareModerationDecisionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload)),
            cancellationToken);
        if (!string.Equals(
                job.Kind,
                ShareModerationDecisionJob.Kind,
                StringComparison.Ordinal)
            || job.PayloadVersion != ShareModerationDecisionJob.PayloadVersion)
        {
            throw new InvalidOperationException("The persisted moderation decision payload version is invalid.");
        }

        try
        {
            ShareModerationDecisionJobPayload persistedPayload =
                job.Payload.Deserialize<ShareModerationDecisionJobPayload>()
                ?? throw new InvalidOperationException(
                    "The persisted moderation decision payload is missing.");
            if (!string.Equals(
                    persistedPayload.ReportId,
                    payload.ReportId,
                    StringComparison.Ordinal)
                || persistedPayload.Decision != payload.Decision
                || persistedPayload.Continuation != payload.Continuation)
            {
                throw new InvalidOperationException(
                    "The persisted moderation decision identity is invalid.");
            }

            return persistedPayload;
        }
        catch (JsonException exception)
        {
            throw new InvalidOperationException(
                "The persisted moderation decision payload is invalid.",
                exception);
        }
    }

    public async Task ScheduleContinuationAsync(
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        _ = await this.ScheduleAsync(
            payload with { Continuation = checked(payload.Continuation + 1) },
            cancellationToken);
    }
}
