using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de déverrouillage utilisateur.
/// </summary>
public sealed class UnlockUserCommandHandler : ICommandHandler<UnlockUserCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public UnlockUserCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(UnlockUserCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        User? unlockedUser = await this.userRepository.UnlockAsync(command.UserId, cancellationToken);
        if (unlockedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.CannotUnlockUser());
        }

        return ApplicationResult<User>.Success(unlockedUser);
    }
}
