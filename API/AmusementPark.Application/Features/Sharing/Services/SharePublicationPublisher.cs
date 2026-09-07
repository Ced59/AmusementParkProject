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

    public async Task<ApplicationResult<SharePublicationCommitResult>> PublishAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        long sourceVersion,
        SharePublicationApprovalState approvedPublicationState,
        ShareContentPolicy contentPolicy,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
                ownerUserId,
                publicationType,
                sourceScopeKey,
                cancellationToken);
            if (!approvedPublicationState.Matches(publication))
            {
                return ApplicationResult<SharePublicationCommitResult>.Failure(
                    SharingApplicationErrors.ApprovedPreviewExpired());
            }

            if (publication?.IsResolvable == true
                && publication.SourceVersion == sourceVersion
                && publication.ContentPolicy.HasSameSelectionAs(contentPolicy))
            {
                return Success(publication, wasWritten: false);
            }

            if (publication is null || publication.Status == SharePublicationStatus.Revoked)
            {
                SharePublication created = SharePublication.Create(
                    SharePublicationId.New(),
                    ownerUserId,
                    publicationType,
                    sourceScopeKey,
                    contentPolicy,
                    sourceVersion,
                    nowUtc);
                created.Publish(
                    this.tokenFactory.Generate(),
                    ShareVisibility.Unlisted,
                    sourceVersion,
                    contentPolicy,
                    0,
                    nowUtc);
                SharePublicationWriteOutcome createOutcome = await this.repository.CreateAsync(
                    created,
                    cancellationToken);
                if (createOutcome == SharePublicationWriteOutcome.Success)
                {
                    return Success(created, wasWritten: true);
                }

                continue;
            }

            (SharePublicationWriteOutcome Outcome, long PreparedVersion) preparation =
                await this.PrepareExistingAsync(
                publication,
                contentPolicy,
                sourceVersion,
                nowUtc,
                cancellationToken);
            if (preparation.Outcome != SharePublicationWriteOutcome.Success)
            {
                continue;
            }

            publication = await this.repository.GetOwnedAsync(
                publication.Id,
                ownerUserId,
                cancellationToken);
            if (publication is null || publication.Version != preparation.PreparedVersion)
            {
                continue;
            }

            long expectedVersion = publication.Version;
            ShareToken token = publication.ShareToken ?? this.tokenFactory.Generate();
            publication.Publish(
                token,
                ShareVisibility.Unlisted,
                publication.SourceVersion,
                publication.ContentPolicy,
                publication.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            SharePublicationWriteOutcome publishOutcome = await this.repository.ReplaceAsync(
                publication,
                expectedVersion,
                cancellationToken);
            if (publishOutcome == SharePublicationWriteOutcome.Success)
            {
                return Success(publication, wasWritten: true);
            }
        }

        return ApplicationResult<SharePublicationCommitResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    public async Task RevokeIfUnchangedAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        SharePublicationApprovalState committedState,
        CancellationToken cancellationToken)
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
        await this.repository.ReplaceAsync(
            publication,
            expectedVersion,
            cancellationToken);
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

    private static ApplicationResult<SharePublicationCommitResult> Success(
        SharePublication publication,
        bool wasWritten)
    {
        SharePublicationCommitResult result = new SharePublicationCommitResult(
            SharePublicationSettingsMapper.ToResult(publication),
            SharePublicationApprovalState.From(publication),
            wasWritten);
        return ApplicationResult<SharePublicationCommitResult>.Success(result);
    }
}
