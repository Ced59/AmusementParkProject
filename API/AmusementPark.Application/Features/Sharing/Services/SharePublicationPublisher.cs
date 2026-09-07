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

    public SharePublicationPublisher(
        ISharePublicationRepository repository,
        IShareTokenFactory tokenFactory,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> PublishAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState approvedPublicationState,
        ShareContentPolicy contentPolicy,
        ISharePublicationSourceDescriptor source,
        CancellationToken cancellationToken)
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
            && publication.ContentPolicy.HasSameSelectionAs(contentPolicy);
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
                    nowUtc);
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
            ShareToken token = publication.ShareToken ?? this.tokenFactory.Generate();
            publication.Publish(
                token,
                ShareVisibility.Unlisted,
                sourceVersion,
                contentPolicy,
                publication.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            SharePublicationWriteOutcome publishOutcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (publishOutcome == SharePublicationWriteOutcome.Success)
            {
                return Success(publication);
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

    private async Task<(SharePublicationWriteOutcome Outcome, long PreparedVersion)> PrepareExistingAsync(
        SharePublication publication,
        ShareContentPolicy policy,
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

        if (!publication.ContentPolicy.HasSameSelectionAs(policy))
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
