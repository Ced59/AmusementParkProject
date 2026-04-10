using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de réinitialisation de mot de passe.
/// </summary>
public sealed class ResetPasswordCommandHandler : ICommandHandler<ResetPasswordCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IPasswordHasher passwordHasher;

    public ResetPasswordCommandHandler(IUserRepository userRepository, IRefreshTokenFactory refreshTokenFactory, IPasswordHasher passwordHasher)
    {
        this.userRepository = userRepository;
        this.refreshTokenFactory = refreshTokenFactory;
        this.passwordHasher = passwordHasher;
    }

    public async Task<ApplicationResult> HandleAsync(ResetPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        if (!string.Equals(command.Request.NewPassword, command.Request.VerifyNewPassword, StringComparison.Ordinal))
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordsNotSames());
        }

        if (!UserRules.IsValidPassword(command.Request.NewPassword))
        {
            return ApplicationResult.Failure(UserApplicationErrors.InvalidPassword());
        }

        if (string.IsNullOrWhiteSpace(command.Request.Token))
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordResetTokenInvalid());
        }

        string tokenHash = this.refreshTokenFactory.ComputeHash(command.Request.Token);
        User? user = await this.userRepository.GetByPasswordResetTokenHashAsync(tokenHash, cancellationToken);
        if (user is null)
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordResetTokenInvalid());
        }

        if (!user.PasswordResetTokenExpiresAtUtc.HasValue || user.PasswordResetTokenExpiresAtUtc.Value < DateTime.UtcNow)
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordResetTokenExpired());
        }

        user.HashedPassword = this.passwordHasher.HashPassword(command.Request.NewPassword);
        user.UpdatedAtUtc = DateTime.UtcNow;
        user.LastActivityUtc = DateTime.UtcNow;
        user.PasswordResetTokenHash = null;
        user.PasswordResetTokenExpiresAtUtc = null;
        user.PasswordResetSentAtUtc = null;

        User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
        if (updatedUser is null)
        {
            return ApplicationResult.Failure(UserApplicationErrors.PasswordResetFailed());
        }

        return ApplicationResult.Success();
    }
}
