using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class RevokeProfileComparisonCommandHandler
    : ICommandHandler<RevokeProfileComparisonCommand,
        ApplicationResult<ProfileComparisonRevocationResult>>
{
    private readonly ProfileComparisonLifecycleService lifecycleService;

    public RevokeProfileComparisonCommandHandler(
        ProfileComparisonLifecycleService lifecycleService)
    {
        this.lifecycleService = lifecycleService
            ?? throw new ArgumentNullException(nameof(lifecycleService));
    }

    public Task<ApplicationResult<ProfileComparisonRevocationResult>> HandleAsync(
        RevokeProfileComparisonCommand command,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(command);
        return this.lifecycleService.RevokeAsync(
            command.UserId,
            command.ShareId,
            cancellationToken);
    }
}
