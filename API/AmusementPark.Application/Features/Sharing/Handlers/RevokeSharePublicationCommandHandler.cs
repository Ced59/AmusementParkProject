using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class RevokeSharePublicationCommandHandler
    : ICommandHandler<RevokeSharePublicationCommand, ApplicationResult<SharePublicationSettingsResult>>
{
    private readonly SharePublicationLifecycleService lifecycleService;

    public RevokeSharePublicationCommandHandler(SharePublicationLifecycleService lifecycleService)
    {
        this.lifecycleService = lifecycleService;
    }

    public Task<ApplicationResult<SharePublicationSettingsResult>> HandleAsync(
        RevokeSharePublicationCommand command,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId)
            || string.IsNullOrWhiteSpace(command.PublicationId))
        {
            return Task.FromResult(ApplicationResult<SharePublicationSettingsResult>.Failure(
                ApplicationErrors.Required(string.IsNullOrWhiteSpace(command.UserId)
                    ? nameof(command.UserId)
                    : nameof(command.PublicationId))));
        }

        return this.lifecycleService.RevokeByIdAsync(
            command.UserId,
            command.PublicationId,
            cancellationToken);
    }
}
