using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class SetSharePublicationVisibilityCommandHandler
    : ICommandHandler<SetSharePublicationVisibilityCommand, ApplicationResult<SharePublicationSettingsResult>>
{
    private const int MaximumWriteAttempts = 5;

    private readonly ISharePublicationRepository repository;
    private readonly IShareTokenFactory tokenFactory;
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly TimeProvider timeProvider;

    public SetSharePublicationVisibilityCommandHandler(
        ISharePublicationRepository repository,
        IShareTokenFactory tokenFactory,
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
        this.tokenFactory = tokenFactory ?? throw new ArgumentNullException(nameof(tokenFactory));
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.timeProvider = timeProvider ?? TimeProvider.System;
    }

    public async Task<ApplicationResult<SharePublicationSettingsResult>> HandleAsync(
        SetSharePublicationVisibilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                ApplicationErrors.Required(nameof(command.UserId)));
        }

        if (!this.sources.TryGetValue(
                command.PublicationType,
                out ISharePublicationSourceDescriptor? source))
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.InvalidPublicationType());
        }

        string ownerUserId = command.UserId.Trim();
        ApplicationResult<string> scopeResult = source.ResolveSourceScopeKey(
            ownerUserId,
            command.SourceId);
        if (!scopeResult.IsSuccess || scopeResult.Value is null)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(scopeResult.Errors);
        }

        return command.IsPublic
            ? await this.PublishAsync(ownerUserId, source, scopeResult.Value, cancellationToken)
            : await this.RevokeAsync(
                ownerUserId,
                command.PublicationType,
                scopeResult.Value,
                cancellationToken);
    }

    private async Task<ApplicationResult<SharePublicationSettingsResult>> PublishAsync(
        string ownerUserId,
        ISharePublicationSourceDescriptor source,
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        ApplicationResult<long> versionResult = await source.GetCurrentSourceVersionAsync(
            sourceScopeKey,
            cancellationToken);
        if (!versionResult.IsSuccess)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(versionResult.Errors);
        }

        ShareContentPolicy defaultPolicy = source.CreateDefaultPolicy();
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            DateTime nowUtc = this.timeProvider.GetUtcNow().UtcDateTime;
            SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
                ownerUserId,
                source.PublicationType,
                sourceScopeKey,
                cancellationToken);
            if (publication?.IsResolvable == true)
            {
                return Success(publication);
            }

            if (publication is null || publication.Status == SharePublicationStatus.Revoked)
            {
                SharePublication created = SharePublication.Create(
                    SharePublicationId.New(),
                    ownerUserId,
                    source.PublicationType,
                    sourceScopeKey,
                    defaultPolicy,
                    versionResult.Value,
                    nowUtc);
                created.Publish(
                    this.tokenFactory.Generate(),
                    ShareVisibility.Unlisted,
                    versionResult.Value,
                    defaultPolicy,
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
                defaultPolicy,
                versionResult.Value,
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

    private async Task<ApplicationResult<SharePublicationSettingsResult>> RevokeAsync(
        string ownerUserId,
        SharePublicationType publicationType,
        string sourceScopeKey,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt < MaximumWriteAttempts; attempt++)
        {
            SharePublication? publication = await this.repository.GetOwnedBySourceAsync(
                ownerUserId,
                publicationType,
                sourceScopeKey,
                cancellationToken);
            if (publication is null
                || publication.Status is SharePublicationStatus.Draft or SharePublicationStatus.Revoked)
            {
                return Success(null);
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
                return Success(publication);
            }
        }

        return ApplicationResult<SharePublicationSettingsResult>.Failure(
            SharingApplicationErrors.PublicationChangedConcurrently());
    }

    private static ApplicationResult<SharePublicationSettingsResult> Success(
        SharePublication? publication)
    {
        return ApplicationResult<SharePublicationSettingsResult>.Success(
            SharePublicationSettingsMapper.ToResult(publication));
    }
}
