using System.Text.Json;
using AmusementPark.Application.Errors;
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
    private readonly IProfileComparisonRepository comparisonRepository;
    private readonly ISharePublicationCacheInvalidationExecutor executor;
    private readonly SharePublicationCacheInvalidationScheduler scheduler;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSnapshotWriter>
        snapshotWriters;

    public SharePublicationCacheInvalidationJobHandler(
        ISharePublicationRepository publicationRepository,
        IProfileComparisonRepository comparisonRepository,
        ISharePublicationCacheInvalidationExecutor executor,
        SharePublicationCacheInvalidationScheduler scheduler,
        IEnumerable<ISharePublicationSnapshotWriter> snapshotWriters)
    {
        this.publicationRepository = publicationRepository
            ?? throw new ArgumentNullException(nameof(publicationRepository));
        this.comparisonRepository = comparisonRepository
            ?? throw new ArgumentNullException(nameof(comparisonRepository));
        this.executor = executor ?? throw new ArgumentNullException(nameof(executor));
        this.scheduler = scheduler ?? throw new ArgumentNullException(nameof(scheduler));
        ArgumentNullException.ThrowIfNull(snapshotWriters);
        this.snapshotWriters = snapshotWriters.ToDictionary(
            static writer => writer.PublicationType);
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
            || payload.SnapshotCleanupPublicationVersion is <= 0
            || payload.ShareIds is null
            || payload.ShareIds.Count == 0)
        {
            return DurableBackgroundJobHandlerResult.DeadLetter(
                "sharing-cache-invalidation.invalid-payload");
        }

        try
        {
            SharePublication? publication = null;
            if (payload.PublicationType == SharePublicationType.ProfileComparison)
            {
                ProfileComparisonId comparisonId = ProfileComparisonId.Parse(
                    payload.PublicationId);
                ProfileComparison? comparison = await this.comparisonRepository.GetByIdAsync(
                    comparisonId,
                    cancellationToken);
                if (comparison is not null
                    && (comparison.Version < payload.MinimumPublicationStateVersion
                        || comparison.IsActive))
                {
                    return DurableBackgroundJobHandlerResult.Retry(
                        "sharing-cache-invalidation.state-not-committed");
                }
            }
            else
            {
                publication = await this.publicationRepository.GetOwnedAsync(
                    publicationId,
                    payload.OwnerUserId,
                    cancellationToken);
                if (publication is not null
                    && publication.Version < payload.MinimumPublicationStateVersion)
                {
                    return DurableBackgroundJobHandlerResult.Retry(
                        "sharing-cache-invalidation.state-not-committed");
                }
            }

            bool succeeded = await this.executor.TryInvalidateAsync(
                new SharePublicationCacheInvalidationRequest(
                    payload.PublicationId,
                    payload.PublicationType,
                    payload.ShareIds),
                cancellationToken);
            if (!succeeded)
            {
                return await this.RetryOrContinueAsync(
                    payload,
                    context.AttemptCount,
                    "sharing-cache-invalidation.not-confirmed",
                    cancellationToken);
            }

            if (payload.SnapshotCleanupPublicationVersion.HasValue
                && publication is not null)
            {
                if (!this.snapshotWriters.TryGetValue(
                        payload.PublicationType,
                        out ISharePublicationSnapshotWriter? snapshotWriter))
                {
                    return await this.RetryOrContinueAsync(
                        payload,
                        context.AttemptCount,
                        "sharing-cache-invalidation.snapshot-cleanup-unavailable",
                        cancellationToken);
                }

                ApplicationResult<bool> cleanup = await snapshotWriter.DeleteSupersededAsync(
                    publicationId,
                    payload.SnapshotCleanupPublicationVersion.Value,
                    cancellationToken);
                if (!cleanup.IsSuccess)
                {
                    return await this.RetryOrContinueAsync(
                        payload,
                        context.AttemptCount,
                        "sharing-cache-invalidation.snapshot-cleanup-failed",
                        cancellationToken);
                }
            }

            return DurableBackgroundJobHandlerResult.Success();
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
                "sharing-cache-invalidation.dependency-unavailable",
                cancellationToken);
        }
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
