using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationLifecycleService
{
    private const int MaximumWriteAttempts = 5;

    private readonly ISharePublicationRepository repository;
    private readonly IShareTokenFactory tokenFactory;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSnapshotWriter> snapshotWriters;
    private readonly SharePublicationCacheInvalidationScheduler? invalidationScheduler;
    private readonly TimeProvider timeProvider;

    public SharePublicationLifecycleService(
        ISharePublicationRepository repository,
        IShareTokenFactory tokenFactory,
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        IEnumerable<ISharePublicationSnapshotWriter>? snapshotWriters = null,
        SharePublicationCacheInvalidationScheduler? invalidationScheduler = null,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.snapshotWriters = (snapshotWriters ?? Array.Empty<ISharePublicationSnapshotWriter>())
            .ToDictionary(static writer => writer.PublicationType);
        this.invalidationScheduler = invalidationScheduler;
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> RotateAsync(
        string ownerUserId,
        string publicationId,
        CancellationToken cancellationToken)
    {
        if (!SharePublicationId.TryParse(publicationId, out SharePublicationId parsedPublicationId))
        {
            return NotFound();
        }

        string normalizedOwnerUserId = ownerUserId.Trim();
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            SharePublication? publication = await this.repository.GetOwnedAsync(
                parsedPublicationId,
                normalizedOwnerUserId,
                cancellationToken);
            if (publication is null)
            {
                return NotFound();
            }

            if (!publication.IsResolvable
                || publication.ShareToken is null
                || !this.sources.TryGetValue(
                    publication.Type,
                    out ISharePublicationSourceDescriptor? source))
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.PublicationNotRotatable());
            }

            ApplicationResult<long> currentSourceVersion = await source.GetCurrentSourceVersionAsync(
                new SharePublicationSourceVersionRequest(
                    publication.SourceScopeKey,
                    publication.ContentPolicy,
                    publication.Id,
                    publication.PublicationVersion),
                cancellationToken);
            if (!currentSourceVersion.IsSuccess)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    currentSourceVersion.Errors);
            }

            if (currentSourceVersion.Value != publication.SourceVersion)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.ApprovedPreviewExpired());
            }

            long sourcePublicationVersion = publication.PublicationVersion;
            long targetPublicationVersion = checked(sourcePublicationVersion + 1);
            ISharePublicationSnapshotWriter? snapshotWriter = null;
            if (this.snapshotWriters.TryGetValue(
                    publication.Type,
                    out snapshotWriter))
            {
                ApplicationResult<bool> cloneResult = await snapshotWriter.CloneAsync(
                    new SharePublicationSnapshotCloneRequest(
                        publication.Id,
                        sourcePublicationVersion,
                        targetPublicationVersion,
                        publication.Version,
                        publication.SourceVersion,
                        publication.ContentPolicy,
                        publication.ContentFingerprint),
                    cancellationToken);
                if (!cloneResult.IsSuccess)
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        cloneResult.Errors);
                }
            }
            else if (publication.Type != SharePublicationType.PersonalRanking)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.SnapshotUnavailable());
            }

            string previousShareId = publication.ShareToken.Value.Value;
            long expectedVersion = publication.Version;
            publication.RotateToken(
                this.tokenFactory.Generate(),
                sourcePublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            await this.ScheduleRotationInvalidationAsync(
                publication,
                snapshotWriter is not null,
                cancellationToken,
                previousShareId,
                publication.ShareToken!.Value.Value);
            SharePublicationWriteOutcome outcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (outcome == SharePublicationWriteOutcome.TokenCollision)
            {
                continue;
            }

            if (outcome != SharePublicationWriteOutcome.Success)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.PublicationChangedConcurrently());
            }

            ApplicationResult<long> confirmedSourceVersion;
            try
            {
                confirmedSourceVersion = await source.GetCurrentSourceVersionAsync(
                    new SharePublicationSourceVersionRequest(
                        publication.SourceScopeKey,
                        publication.ContentPolicy,
                        publication.Id,
                        publication.PublicationVersion),
                    cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                return Success(publication);
            }

            if (!confirmedSourceVersion.IsSuccess
                || confirmedSourceVersion.Value != publication.SourceVersion)
            {
                await this.RevokeIfCurrentAsync(publication, cancellationToken);
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.ApprovedPreviewExpired());
            }

            return Success(publication);
        }

        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> RevokeByIdAsync(
        string ownerUserId,
        string publicationId,
        CancellationToken cancellationToken)
    {
        if (!SharePublicationId.TryParse(publicationId, out SharePublicationId parsedPublicationId))
        {
            return NotFound();
        }

        SharePublication? publication = await this.repository.GetOwnedAsync(
            parsedPublicationId,
            ownerUserId.Trim(),
            cancellationToken);
        return publication is null
            ? NotFound()
            : await this.RevokeAsync(publication, cancellationToken);
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> RevokeBySourceAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
            ownerUserId.Trim(),
            publicationType,
            sourceScopeKey,
            cancellationToken);
        return publication is null
            ? Success(null)
            : await this.RevokeAsync(publication, cancellationToken);
    }

    private async Task<ApplicationResult<SharePublicationSettingsResult>> RevokeAsync(
        SharePublication initialPublication,
        CancellationToken cancellationToken)
    {
        SharePublication publication = initialPublication;
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            if (publication.Status is SharePublicationStatus.Draft or SharePublicationStatus.Revoked)
            {
                return Success(publication);
            }

            string? previousShareId = publication.ShareToken?.Value;
            long expectedVersion = publication.Version;
            publication.Revoke(
                publication.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            await this.ScheduleInvalidationAsync(
                publication,
                cancellationToken,
                previousShareId);
            SharePublicationWriteOutcome outcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (outcome == SharePublicationWriteOutcome.Success)
            {
                return Success(publication);
            }

            SharePublication? current = await this.repository.GetOwnedAsync(
                publication.Id,
                publication.OwnerUserId,
                cancellationToken);
            if (current is null)
            {
                return NotFound();
            }

            publication = current;
        }

        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    private async Task RevokeIfCurrentAsync(
        SharePublication rotatedPublication,
        CancellationToken cancellationToken)
    {
        SharePublication? current = await this.repository.GetOwnedAsync(
            rotatedPublication.Id,
            rotatedPublication.OwnerUserId,
            cancellationToken);
        if (current is null
            || current.Version != rotatedPublication.Version
            || current.Status != SharePublicationStatus.Published)
        {
            return;
        }

        string? shareId = current.ShareToken?.Value;
        long expectedVersion = current.Version;
        current.Revoke(
            current.PublicationVersion,
            this.timeProvider.GetUtcNow().UtcDateTime);
        await this.ScheduleInvalidationAsync(
            current,
            cancellationToken,
            shareId);
        _ = await this.repository.ReplaceAsync(
            current,
            expectedVersion,
            cancellationToken);
    }

    private Task ScheduleInvalidationAsync(
        SharePublication publication,
        CancellationToken cancellationToken,
        params string?[] shareIds)
    {
        return this.invalidationScheduler?.ScheduleAsync(
                publication,
                publication.Version,
                cancellationToken,
                shareIds)
            ?? Task.CompletedTask;
    }

    private Task ScheduleRotationInvalidationAsync(
        SharePublication publication,
        bool cleanupSnapshots,
        CancellationToken cancellationToken,
        params string?[] shareIds)
    {
        return this.invalidationScheduler?.ScheduleRotationAsync(
                publication,
                publication.Version,
                cleanupSnapshots,
                cancellationToken,
                shareIds)
            ?? Task.CompletedTask;
    }

    private static ApplicationResult<SharePublicationSettingsResult> Success(
        SharePublication? publication)
    {
        return ApplicationResult<SharePublicationSettingsResult>.Success(
            SharePublicationSettingsMapper.ToResult(publication));
    }

    private static ApplicationResult<SharePublicationSettingsResult> NotFound()
    {
        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.SharedPublicationNotFound());
    }
}
