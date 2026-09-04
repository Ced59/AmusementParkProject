using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Passport.Models;
using AmusementPark.Application.Features.Passport.Ports;
using AmusementPark.Core.Domain.Visits;

namespace AmusementPark.Application.Features.Passport.Services;

public sealed class VisitPurgeJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumAttempts = 100;
    private readonly IVisitDeletionStore deletionStore;
    private readonly IPassportClock clock;

    public VisitPurgeJobHandler(
        IVisitDeletionStore deletionStore,
        IPassportClock clock)
    {
        this.deletionStore = deletionStore;
        this.clock = clock;
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            VisitPurgeJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { VisitPurgeJob.PayloadVersion },
            TimeSpan.FromMinutes(2),
            MaximumAttempts,
            TimeSpan.FromSeconds(5),
            TimeSpan.FromMinutes(5),
            maximumConcurrency: 1);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        VisitPurgeJobPayload? payload = Deserialize(context);
        if (payload is null
            || !VisitId.TryParse(payload.VisitId, out VisitId visitId))
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                "passport-visit-purge.invalid-payload");
        }

        VisitDeletionPurgeResult result = await this.deletionStore.PurgeBatchAsync(
            visitId,
            payload.UserId,
            this.clock.UtcNow,
            VisitDeletionPolicy.PurgeBatchSize,
            cancellationToken);
        return result.IsCompleted
            ? DurableBackgroundJobHandlerResult.Success()
            : DurableBackgroundJobHandlerResult.Retry(
                "passport-visit-purge.remaining-documents");
    }

    private static VisitPurgeJobPayload? Deserialize(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != VisitPurgeJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            VisitPurgeJobPayload? payload = context.Payload.Deserialize<VisitPurgeJobPayload>();
            return payload is not null && !string.IsNullOrWhiteSpace(payload.UserId)
                ? payload
                : null;
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
