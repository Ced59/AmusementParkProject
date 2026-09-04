using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class VisitPurgeScheduler
{
    private readonly IDurableBackgroundJobRepository jobRepository;

    public VisitPurgeScheduler(IDurableBackgroundJobRepository jobRepository)
    {
        this.jobRepository = jobRepository;
    }

    public Task<DurableBackgroundJob> ScheduleAsync(
        VisitId visitId,
        string userId,
        long deletionVersion,
        TimeSpan delay,
        CancellationToken cancellationToken)
    {
        VisitPurgeJobPayload payload = new VisitPurgeJobPayload(
            visitId.Value,
            userId,
            deletionVersion,
            0);
        return this.ScheduleAsync(payload, delay, cancellationToken);
    }

    public Task<DurableBackgroundJob> ScheduleContinuationAsync(
        VisitPurgeJobPayload completedPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(completedPayload);
        VisitPurgeJobPayload continuation = completedPayload with
        {
            Continuation = checked(completedPayload.Continuation + 1),
        };
        return this.ScheduleAsync(continuation, null, cancellationToken);
    }

    private Task<DurableBackgroundJob> ScheduleAsync(
        VisitPurgeJobPayload payload,
        TimeSpan? delay,
        CancellationToken cancellationToken)
    {
        return this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                VisitPurgeJob.Kind,
                $"passport-visit-purge:{payload.VisitId}:{payload.DeletionVersion}:{payload.Continuation}",
                VisitPurgeJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload),
                Delay: delay),
            cancellationToken);
    }
}
