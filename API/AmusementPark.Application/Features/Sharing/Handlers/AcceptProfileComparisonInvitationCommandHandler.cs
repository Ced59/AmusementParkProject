using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class AcceptProfileComparisonInvitationCommandHandler
    : ICommandHandler<AcceptProfileComparisonInvitationCommand,
        ApplicationResult<ProfileComparisonInvitationAcceptanceResult>>
{
    private readonly ProfileComparisonInvitationService service;

    public AcceptProfileComparisonInvitationCommandHandler(
        ProfileComparisonInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<ProfileComparisonInvitationAcceptanceResult>> HandleAsync(
        AcceptProfileComparisonInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.AcceptAsync(command.UserId, command.Token, cancellationToken);
    }
}
