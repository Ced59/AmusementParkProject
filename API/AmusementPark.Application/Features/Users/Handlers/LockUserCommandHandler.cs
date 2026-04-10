using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de verrouillage utilisateur.
/// </summary>
public sealed class LockUserCommandHandler : ICommandHandler<LockUserCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;

    public LockUserCommandHandler(IUserRepository userRepository)
    {
        this.userRepository = userRepository;
    }

    public async Task<ApplicationResult<User>> HandleAsync(LockUserCommand command, CancellationToken cancellationToken = default)
    {
        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        User? lockedUser = await this.userRepository.LockAsync(command.UserId, cancellationToken);
        if (lockedUser is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.CannotLockUser());
        }

        return ApplicationResult<User>.Success(lockedUser);
    }
}
