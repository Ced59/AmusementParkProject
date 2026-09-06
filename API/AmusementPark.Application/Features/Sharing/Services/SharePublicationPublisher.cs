using AmusementPark.Application.Errors;
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
            if (publication?.IsResolvable == true
                && publication.SourceVersion == sourceVersion
                && publication.ContentPolicy.HasSameSelectionAs(contentPolicy))
            {
                return Success(publication);
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
                    return Success(created);
                }

                continue;
            }

            SharePublicationWriteOutcome preparationOutcome = await this.PrepareExistingAsync(
                publication,
                contentPolicy,
                sourceVersion,
                nowUtc,
                cancellationToken);
            if (preparationOutcome != SharePublicationWriteOutcome.Success)
            {
                continue;
            }

            publication = await this.repository.GetOwnedAsync(
                publication.Id,
                ownerUserId,
                cancellationToken);
            if (publication is null)
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
                return Success(publication);
            }
        }

        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    private async Task<SharePublicationWriteOutcome> PrepareExistingAsync(
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
                return sourceOutcome;
            }
        }

        if (!publication.ContentPolicy.HasSameSelectionAs(policy))
        {
            SharePublication? current = await this.repository.GetOwnedAsync(
                publication.Id,
                publication.OwnerUserId,
                cancellationToken);
            if (current is null)
            {
                return SharePublicationWriteOutcome.Conflict;
            }

            long expectedVersion = current.Version;
            current.ReplaceContentPolicy(
                policy,
                current.PublicationVersion,
                this.timeProvider.GetUtcNow().UtcDateTime);
            return await this.repository.ReplaceAsync(
                current,
                expectedVersion,
                cancellationToken);
        }

        return SharePublicationWriteOutcome.Success;
    }

    private static ApplicationResult<SharePublicationSettingsResult> Success(
        SharePublication publication)
    {
        return ApplicationResult<SharePublicationSettingsResult>.Success(
            SharePublicationSettingsMapper.ToResult(publication));
    }
}
