using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'assignation de rôle.
/// </summary>
public sealed class AssignRoleCommandHandler : ICommandHandler<AssignRoleCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard)
    {
        this.userRepository = userRepository;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
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

        ShareSourceMutationLease mutationLease =
            await this.shareSourceRevisionGuard.BeginMutationAsync(user.Id, cancellationToken);
        using CancellationTokenSource mutationCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                cancellationToken,
                mutationLease);
        bool sourceMutationAttempted = false;
        User? updatedUser;
        try
        {
            sourceMutationAttempted = true;
            updatedUser = await this.userRepository.AssignRoleAsync(
                command.UserId,
                command.Role,
                mutationCancellation.Token);
        }
        finally
        {
            await this.shareSourceRevisionGuard.CompleteMutationAsync(
                mutationLease,
                sourceMutationAttempted,
                CancellationToken.None);
        }
        if (updatedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.AssignRoleFailed());
        }

        return ApplicationResult<User>.Success(updatedUser);
    }
}
