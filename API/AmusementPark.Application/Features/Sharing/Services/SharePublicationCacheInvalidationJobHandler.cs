using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationCacheInvalidationJobHandler : IDurableBackgroundJobHandler
{
    private const int MaximumAttempts = 100;
    private const int ContinuationAttemptThreshold = MaximumAttempts / 2;
    private readonly ISharePublicationRepository publicationRepository;
    private readonly ISharePublicationCacheInvalidationExecutor executor;
    private readonly SharePublicationCacheInvalidationScheduler scheduler;

    public SharePublicationCacheInvalidationJobHandler(
        ISharePublicationRepository publicationRepository,
        ISharePublicationCacheInvalidationExecutor executor,
        SharePublicationCacheInvalidationScheduler scheduler)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            SharePublicationCacheInvalidationJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { SharePublicationCacheInvalidationJob.PayloadVersion },
            TimeSpan.FromMinutes(1),
            maximumAttempts: MaximumAttempts,
            initialRetryDelay: TimeSpan.FromSeconds(10),
            maximumRetryDelay: TimeSpan.FromMinutes(5),
            maximumConcurrency: 2);

    public async Task<DurableBackgroundJobHandlerResult> HandleAsync(
        DurableBackgroundJobExecutionContext context,
        CancellationToken cancellationToken)
    {
        SharePublicationCacheInvalidationJobPayload? payload = Deserialize(context);
        if (payload is null
            || !SharePublicationId.TryParse(payload.PublicationId, out SharePublicationId publicationId)
            || string.IsNullOrWhiteSpace(payload.OwnerUserId)
            || !Enum.IsDefined(payload.PublicationType)
            || payload.MinimumPublicationStateVersion < 0
            || payload.Continuation < 0
            || payload.ShareIds is null
            || payload.ShareIds.Count == 0)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                "sharing-cache-invalidation.invalid-payload");
        }

        SharePublication? publication = await this.publicationRepository.GetOwnedAsync(
            publicationId,
            payload.OwnerUserId,
            cancellationToken);
        if (publication is not null
            && publication.Version < payload.MinimumPublicationStateVersion)
        {
            return DurableBackgroundJobHandlerResult.Retry(
                "sharing-cache-invalidation.state-not-committed");
        }

        bool succeeded = await this.executor.TryInvalidateAsync(
            new SharePublicationCacheInvalidationRequest(
                payload.PublicationId,
                payload.PublicationType,
                payload.ShareIds),
            cancellationToken);
        if (succeeded)
        {
            return DurableBackgroundJobHandlerResult.Success();
        }

        return await this.RetryOrContinueAsync(
            payload,
            context.AttemptCount,
            "sharing-cache-invalidation.not-confirmed",
            cancellationToken);
    }

    private async Task<DurableBackgroundJobHandlerResult> RetryOrContinueAsync(
        SharePublicationCacheInvalidationJobPayload payload,
        int attemptCount,
        string retryErrorCode,
        CancellationToken cancellationToken)
    {
        if (attemptCount < ContinuationAttemptThreshold)
        {
            return DurableBackgroundJobHandlerResult.Retry(retryErrorCode);
        }

        await this.scheduler.ScheduleContinuationAsync(payload, cancellationToken);
        return DurableBackgroundJobHandlerResult.Success();
    }

    private static SharePublicationCacheInvalidationJobPayload? Deserialize(
        DurableBackgroundJobExecutionContext context)
    {
        if (context.PayloadVersion != SharePublicationCacheInvalidationJob.PayloadVersion)
        {
            return null;
        }

        try
        {
            return context.Payload.Deserialize<SharePublicationCacheInvalidationJobPayload>();
        }
        catch (JsonException)
        {
            return null;
        }
    }
}
