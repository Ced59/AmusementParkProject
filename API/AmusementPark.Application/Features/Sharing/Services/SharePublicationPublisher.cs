using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Services;

public sealed class SharePublicationPublisher
{
    private const int MaximumWriteAttempts = 5;

    private readonly ISharePublicationRepository repository;
    private readonly IShareTokenFactory tokenFactory;
    private readonly TimeProvider timeProvider;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSnapshotWriter> snapshotWriters;

    public SharePublicationPublisher(
        ISharePublicationRepository repository,
        IShareTokenFactory tokenFactory,
        TimeProvider? timeProvider = null,
        IEnumerable<ISharePublicationSnapshotWriter>? snapshotWriters = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        this.timeProvider = timeProvider ?? TimeProvider.System;
        this.snapshotWriters = (snapshotWriters ?? Array.Empty<ISharePublicationSnapshotWriter>())
            .ToDictionary(static writer => writer.PublicationType);
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> PublishAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState approvedPublicationState,
        ShareContentPolicy contentPolicy,
        ISharePublicationSourceDescriptor source,
        CancellationToken cancellationToken,
        string contentFingerprint = "",
        VisitRecapShareInput? visitRecap = null,
        string? sourceId = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
        SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
            ownerUserId,
            publicationType,
            sourceScopeKey,
            cancellationToken);
        if (!approvedPublicationState.Matches(publication))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired());
        }

        bool alreadyPublished = publication?.IsResolvable == true
            && publication.SourceVersion == sourceVersion
            && publication.ContentPolicy.HasSameSelectionAs(contentPolicy)
            && string.Equals(
                publication.ContentFingerprint,
                contentFingerprint,
                StringComparison.Ordinal);
        if (!alreadyPublished)
        {
            if (publication is null || publication.Status == SharePublicationStatus.Revoked)
            {
                publication = SharePublication.Create(
                    SharePublicationId.New(),
                    ownerUserId,
                    publicationType,
                    sourceScopeKey,
                    contentPolicy,
                    sourceVersion,
                    nowUtc,
                    contentFingerprint);
                SharePublicationWriteOutcome createOutcome = await this.repository.CreateAsync(
                    publication,
                    cancellationToken);
                if (createOutcome != SharePublicationWriteOutcome.Success)
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        SharingApplicationErrors.PublicationChangedConcurrently());
                }
            }
            else
            {
                (SharePublicationWriteOutcome Outcome, long PreparedVersion) preparation =
                    await this.PrepareExistingAsync(
                        publication,
                        contentPolicy,
                        contentFingerprint,
                        sourceVersion,
                        nowUtc,
                        cancellationToken);
                if (preparation.Outcome != SharePublicationWriteOutcome.Success)
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        SharingApplicationErrors.PublicationChangedConcurrently());
                }

                publication = await this.repository.GetOwnedAsync(
                    publication.Id,
                    ownerUserId,
                    cancellationToken);
                if (publication is null || publication.Version != preparation.PreparedVersion)
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        SharingApplicationErrors.ApprovedPreviewExpired());
                }
            }
        }

        ApplicationResult<long> finalSourceVersion = await source.GetCurrentSourceVersionAsync(
            sourceScopeKey,
            cancellationToken);
        if (!finalSourceVersion.IsSuccess)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                finalSourceVersion.Errors);
        }

        if (finalSourceVersion.Value != sourceVersion)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired());
        }

        if (alreadyPublished)
        {
            return Success(publication!);
        }

        SharePublicationApprovalState preparedState = SharePublicationApprovalState.From(publication);
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            if (attempt > 0)
            {
                publication = await this.repository.GetOwnedBySourceAsync(
                    ownerUserId,
                    publicationType,
                    sourceScopeKey,
                    cancellationToken);
            }

            if (!preparedState.Matches(publication))
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.ApprovedPreviewExpired());
            }

            long expectedVersion = publication!.Version;
            if (this.snapshotWriters.TryGetValue(
                    publicationType,
                    out ISharePublicationSnapshotWriter? snapshotWriter))
            {
                if (string.IsNullOrWhiteSpace(sourceId))
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        SharingApplicationErrors.InvalidSource());
                }

                ApplicationResult<bool> snapshotResult = await snapshotWriter.WriteAsync(
                    new SharePublicationSnapshotWriteRequest(
                        publication.Id,
                        checked(publication.PublicationVersion + 1),
                        publication.Version,
                        ownerUserId,
                        sourceId.Trim(),
                        sourceVersion,
                        contentPolicy,
                        contentFingerprint,
                        visitRecap),
                    cancellationToken);
                if (!snapshotResult.IsSuccess)
                {
                    return ApplicationResult<SharePublicationSettingsResult>.Failure(
                        snapshotResult.Errors);
                }
            }

            ShareToken token = publication.Status == SharePublicationStatus.NeedsReview
                ? this.tokenFactory.Generate()
                : publication.ShareToken ?? this.tokenFactory.Generate();
            publication.Publish(
                token,
                ShareVisibility.Unlisted,
                sourceVersion,
                contentPolicy,
                publication.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime,
                contentFingerprint);
            SharePublicationWriteOutcome publishOutcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (publishOutcome == SharePublicationWriteOutcome.Success)
            {
                return await this.ConfirmPublishedSourceAsync(
                    publication,
                    source,
                    sourceScopeKey,
                    sourceVersion,
                    cancellationToken);
            }

            if (publishOutcome != SharePublicationWriteOutcome.TokenCollision)
            {
                return ApplicationResult<SharePublicationSettingsResult>.Failure(
                    SharingApplicationErrors.PublicationChangedConcurrently());
            }
        }

        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    private async Task<ApplicationResult<SharePublicationSettingsResult>> ConfirmPublishedSourceAsync(
        SharePublication publication,
        ISharePublicationSourceDescriptor source,
        string sourceScopeKey,
        long sourceVersion,
        CancellationToken cancellationToken)
    {
        ApplicationResult<long> persistedSourceVersion = await source.GetCurrentSourceVersionAsync(
            sourceScopeKey,
            cancellationToken);
        if (persistedSourceVersion.IsSuccess
            && persistedSourceVersion.Value == sourceVersion)
        {
            return Success(publication);
        }

        SharePublicationApprovalState committedState = SharePublicationApprovalState.From(publication);
        await this.RevokeIfUnchangedAsync(
            publication.OwnerUserId,
            publication.Type,
            publication.SourceScopeKey,
            committedState,
            cancellationToken);

        return persistedSourceVersion.IsSuccess
            ? ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.ApprovedPreviewExpired())
            : ApplicationResult<SharePublicationSettingsResult>.Failure(
                persistedSourceVersion.Errors);
    }

    private async Task RevokeIfUnchangedAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        SharePublicationApprovalState committedState,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            try
            {
                SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
                    ownerUserId,
                    publicationType,
                    sourceScopeKey,
                    cancellationToken);
                if (!committedState.Matches(publication)
                    || publication?.Status != SharePublicationStatus.Published)
                {
                    return;
                }

                long expectedVersion = publication.Version;
                publication.Revoke(
                    publication.PublicationVersion,
                    this.timeProvider.GetUtcNow().UtcDateTime);
                SharePublicationWriteOutcome outcome = await this.repository.ReplaceAsync(
                    publication,
                    expectedVersion,
                    cancellationToken);
                if (outcome == SharePublicationWriteOutcome.Success)
                {
                    return;
                }
            }
            catch (Exception exception) when (exception is not OperationCanceledException)
            {
                // The response remains a source-change failure. A later retry rereads the exact
                // committed state, while public resolution stays closed on the unstable source.
            }
        }
    }

    private async Task<(SharePublicationWriteOutcome Outcome, long PreparedVersion)> PrepareExistingAsync(
        SharePublication publication,
        ShareContentPolicy policy,
        string contentFingerprint,
        long sourceVersion,
        DateTime nowUtc,
        CancellationToken cancellationToken)
    {
        if (publication.SourceVersion != sourceVersion)
        {
            long expectedVersion = publication.Version;
            publication.MarkSourceChanged(sourceVersion, nowUtc);
            SharePublicationWriteOutcome sourceOutcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (sourceOutcome != SharePublicationWriteOutcome.Success)
            {
                return (sourceOutcome, publication.Version);
            }
        }

        long preparedVersion = publication.Version;

        if (!publication.ContentPolicy.HasSameSelectionAs(policy)
            || !string.Equals(
                publication.ContentFingerprint,
                contentFingerprint,
                StringComparison.Ordinal))
        {
            SharePublication? current = await this.repository.GetOwnedAsync(
                publication.Id,
                publication.OwnerUserId,
                cancellationToken);
            if (current is null || current.Version != preparedVersion)
            {
                return (SharePublicationWriteOutcome.Conflict, preparedVersion);
            }

            long expectedVersion = current.Version;
            current.ReplaceContentPolicy(
                policy,
                current.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            current.ReplaceContentFingerprint(
                contentFingerprint,
                current.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            SharePublicationWriteOutcome policyOutcome = await this.repository.ReplaceAsync(
                current,
                expectedVersion,
                cancellationToken);
            return (policyOutcome, current.Version);
        }

        return (SharePublicationWriteOutcome.Success, preparedVersion);
    }

    private static ApplicationResult<SharePublicationSettingsResult> Success(
        SharePublication publication)
    {
        return ApplicationResult<SharePublicationSettingsResult>.Success(
            SharePublicationSettingsMapper.ToResult(publication));
    }
}
