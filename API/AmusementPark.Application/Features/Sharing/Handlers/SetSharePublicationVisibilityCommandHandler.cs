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
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly TimeProvider timeProvider;

    public SetSharePublicationVisibilityCommandHandler(
        ISharePublicationRepository repository,
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        TimeProvider? timeProvider = null)
    {
        this.repository = repository ?? throw new ArgumentNullException(nameof(repository));
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

        if (command.IsPublic)
        {
            return ApplicationResult<SharePublicationSettingsResult>.Failure(
                SharingApplicationErrors.PreviewApprovalRequired());
        }

        return await this.RevokeAsync(
            ownerUserId,
            command.PublicationType,
            scopeResult.Value,
            cancellationToken);
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
