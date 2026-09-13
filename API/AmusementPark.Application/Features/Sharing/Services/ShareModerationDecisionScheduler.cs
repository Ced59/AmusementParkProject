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

    public async Task ScheduleAsync(
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        await this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                ShareModerationDecisionJob.Kind,
                $"share-moderation:{payload.ReportId}:{payload.Decision}:continuation:{payload.Continuation}",
                ShareModerationDecisionJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload)),
            cancellationToken);
    }

    public Task ScheduleContinuationAsync(
        ShareModerationDecisionJobPayload payload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(payload);
        return this.ScheduleAsync(
            payload with { Continuation = checked(payload.Continuation + 1) },
            cancellationToken);
    }
}
