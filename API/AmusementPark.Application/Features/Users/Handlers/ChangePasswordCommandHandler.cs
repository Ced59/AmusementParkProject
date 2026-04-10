using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de changement de mot de passe.
/// </summary>
public sealed class ChangePasswordCommandHandler : ICommandHandler<ChangePasswordCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;

    public ChangePasswordCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
    }

    public async Task<ApplicationResult> HandleAsync(ChangePasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        User? user = await this.userRepository.GetByIdAsync(command.UserId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult.Failure(UserApplicationErrors.UserNotExists());
        }

        if (!string.Equals(command.Request.NewPassword, command.Request.VerifyNewPassword, StringComparison.Ordinal))
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordsNotSames());
        }

        if (!UserRules.IsValidPassword(command.Request.NewPassword))
        {
            return ApplicationResult.Failure(UserApplicationErrors.InvalidPassword());
        }

        bool hasLocalPassword = !string.IsNullOrWhiteSpace(user.HashedPassword);
        if (command.ChangeForSelf && hasLocalPassword && !this.passwordHasher.VerifyPassword(command.Request.CurrentPassword, user.HashedPassword!))
        {
            return ApplicationResult.Failure(UserApplicationErrors.IncorrectPassword());
        }

        string passwordHash = this.passwordHasher.HashPassword(command.Request.NewPassword);
        User? updatedUser = await this.userRepository.ChangePasswordAsync(command.UserId, passwordHash, cancellationToken);
        if (updatedUser is null)
        {
            return ApplicationResult.Failure(UserApplicationErrors.ChangePasswordFailed());
        }

        return ApplicationResult.Success();
    }
}
