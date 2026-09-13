using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Commands;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;

namespace AmusementPark.Application.Features.Sharing.Handlers;

public sealed class CreateProfileComparisonInvitationCommandHandler
    : ICommandHandler<CreateProfileComparisonInvitationCommand,
        ApplicationResult<ProfileComparisonInvitationCreationResult>>
{
    private readonly ProfileComparisonInvitationService service;

    public CreateProfileComparisonInvitationCommandHandler(
        ProfileComparisonInvitationService service)
    {
        this.service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<ApplicationResult<ProfileComparisonInvitationCreationResult>> HandleAsync(
        CreateProfileComparisonInvitationCommand command,
        CancellationToken cancellationToken = default)
    {
        return this.service.CreateAsync(command.UserId, command.Categories, cancellationToken);
    }
}
