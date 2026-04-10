using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'assignation de rôle.
/// </summary>
public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public AssignRoleCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(AssignRoleCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        if (user.HasRole(command.Role))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.RoleAlreadyAssigned());
        }

        User? updatedUser = await this.userRepository.AssignRoleAsync(command.UserId, command.Role, cancellationToken);
        if (updatedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.AssignRoleFailed());
        }

        return ApplicationResult<User>.Success(updatedUser);
    }
}
