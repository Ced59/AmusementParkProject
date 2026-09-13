using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using AmusementPark.Application.Features.BackgroundJobs.Models;
using AmusementPark.Application.Features.BackgroundJobs.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationCacheInvalidationScheduler
{
    private readonly IDurableBackgroundJobRepository jobRepository;

    public SharePublicationCacheInvalidationScheduler(
        IDurableBackgroundJobRepository jobRepository)
    {
        this.jobRepository = jobRepository ?? throw new ArgumentNullException(nameof(jobRepository));
    }

    public async Task ScheduleAsync(
        SharePublication publication,
        long minimumPublicationStateVersion,
        CancellationToken cancellationToken,
        params string?[] shareIds)
    {
        await this.ScheduleAsync(
            publication,
            minimumPublicationStateVersion,
            null,
            cancellationToken,
            shareIds);
    }

    public async Task ScheduleRotationAsync(
        SharePublication publication,
        long minimumPublicationStateVersion,
        bool cleanupSnapshots,
        CancellationToken cancellationToken,
        params string?[] shareIds)
    {
        await this.ScheduleAsync(
            publication,
            minimumPublicationStateVersion,
            cleanupSnapshots ? publication.PublicationVersion : null,
            cancellationToken,
            shareIds);
    }

    private async Task ScheduleAsync(
        SharePublication publication,
        long minimumPublicationStateVersion,
        long? snapshotCleanupPublicationVersion,
        CancellationToken cancellationToken,
        params string?[] shareIds)
    {
        ArgumentNullException.ThrowIfNull(publication);
        if (minimumPublicationStateVersion < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(minimumPublicationStateVersion));
        }

        string[] normalizedShareIds = shareIds
            .Where(static shareId => !string.IsNullOrWhiteSpace(shareId))
            .Select(static shareId => shareId!.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static shareId => shareId, StringComparer.Ordinal)
            .ToArray();
        if (normalizedShareIds.Length == 0)
        {
            return;
        }

        SharePublicationCacheInvalidationJobPayload payload =
            new SharePublicationCacheInvalidationJobPayload(
                publication.Id.Value,
                publication.OwnerUserId,
                publication.Type,
                minimumPublicationStateVersion,
                normalizedShareIds,
                0,
                snapshotCleanupPublicationVersion);
        await this.EnqueueAsync(payload, cancellationToken);
    }

    public Task ScheduleContinuationAsync(
        SharePublicationCacheInvalidationJobPayload completedPayload,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(completedPayload);
        SharePublicationCacheInvalidationJobPayload continuation = completedPayload with
        {
            Continuation = checked(completedPayload.Continuation + 1),
        };
        return this.EnqueueAsync(continuation, cancellationToken);
    }

    public Task ScheduleProfileComparisonAsync(
        ProfileComparison comparison,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(comparison);
        SharePublicationCacheInvalidationJobPayload payload =
            new SharePublicationCacheInvalidationJobPayload(
                comparison.Id.Value,
                comparison.RevokedByUserId ?? comparison.CreatorUserId,
                SharePublicationType.ProfileComparison,
                comparison.Version,
                new[] { comparison.ShareToken.Value });
        return this.EnqueueAsync(payload, cancellationToken);
    }

    private async Task EnqueueAsync(
        SharePublicationCacheInvalidationJobPayload payload,
        CancellationToken cancellationToken)
    {
        string digest = Convert.ToHexString(
            SHA256.HashData(Encoding.UTF8.GetBytes(string.Join('\n', payload.ShareIds))))
            .ToLowerInvariant()[..16];
        await this.jobRepository.EnqueueExactAsync(
            new EnqueueExactBackgroundJobRequest(
                SharePublicationCacheInvalidationJob.Kind,
                $"share-cache:{payload.PublicationId}:{payload.MinimumPublicationStateVersion}:{digest}:continuation:{payload.Continuation}",
                SharePublicationCacheInvalidationJob.PayloadVersion,
                JsonSerializer.SerializeToElement(payload)),
            cancellationToken);
    }
}
