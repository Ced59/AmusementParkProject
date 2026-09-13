using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationCacheInvalidationJobHandler : IDurableBackgroundJobHandler
{
    private readonly ISharePublicationRepository publicationRepository;
    private readonly ISharePublicationCacheInvalidationExecutor executor;

    public SharePublicationCacheInvalidationJobHandler(
        ISharePublicationRepository publicationRepository,
        ISharePublicationCacheInvalidationExecutor executor)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
    }

    public DurableBackgroundJobHandlerDefinition Definition { get; } =
        new DurableBackgroundJobHandlerDefinition(
            SharePublicationCacheInvalidationJob.Kind,
            DurableBackgroundJobWorkload.Light,
            new[] { SharePublicationCacheInvalidationJob.PayloadVersion },
            TimeSpan.FromMinutes(1),
            maximumAttempts: 100,
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
        return succeeded
            ? DurableBackgroundJobHandlerResult.Success()
            : DurableBackgroundJobHandlerResult.Retry(
                "sharing-cache-invalidation.not-confirmed");
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
