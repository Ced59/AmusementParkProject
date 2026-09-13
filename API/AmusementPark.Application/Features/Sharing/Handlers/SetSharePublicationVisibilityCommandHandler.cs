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
    private readonly IReadOnlyDictionary<SharePublicationType, ISharePublicationSourceDescriptor> sources;
    private readonly SharePublicationLifecycleService lifecycleService;

    public SetSharePublicationVisibilityCommandHandler(
        IEnumerable<ISharePublicationSourceDescriptor> sources,
        SharePublicationLifecycleService lifecycleService)
    {
        ArgumentNullException.ThrowIfNull(sources);
        this.sources = sources.ToDictionary(static source => source.PublicationType);
        this.lifecycleService = lifecycleService
            ?? throw new ArgumentNullException(nameof(lifecycleService));
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

        return await this.lifecycleService.RevokeBySourceAsync(
            ownerUserId,
            command.PublicationType,
            scopeResult.Value,
            cancellationToken);
    }
}
