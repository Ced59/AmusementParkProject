using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de déverrouillage utilisateur.
/// </summary>
public sealed class UnlockUserCommandHandler : ICommandHandler<UnlockUserCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard;

    public UnlockUserCommandHandler(
        IUserRepository userRepository,
        IPersonalRankingShareSourceRevisionGuard shareSourceRevisionGuard)
    {
        this.userRepository = userRepository;
        this.shareSourceRevisionGuard = shareSourceRevisionGuard;
    }

    public async Task<ApplicationResult<User>> HandleAsync(UnlockUserCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        ShareSourceMutationLease mutationLease =
            await this.shareSourceRevisionGuard.BeginMutationAsync(user.Id, cancellationToken);
        using CancellationTokenSource mutationCancellation =
            ShareSourceMutationCancellation.CreateLinkedSource(
                cancellationToken,
                mutationLease);
        User? unlockedUser = null;
        try
        {
            unlockedUser = await this.userRepository.UnlockAsync(
                command.UserId,
                mutationCancellation.Token);
        }
        finally
        {
            await this.shareSourceRevisionGuard.CompleteMutationAsync(
                mutationLease,
                unlockedUser is not null,
                CancellationToken.None);
        }
        if (unlockedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.CannotUnlockUser());
        }

        return ApplicationResult<User>.Success(unlockedUser);
    }
}
