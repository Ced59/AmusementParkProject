using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'oubli de mot de passe.
/// </summary>
public sealed class ForgotPasswordCommandHandler : ICommandHandler<ForgotPasswordCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly ILocalAccountEmailService localAccountEmailService;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public ForgotPasswordCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenFactory refreshTokenFactory,
        ILocalAccountEmailService localAccountEmailService,
        IUserAuthenticationSettings authenticationSettings)
    {
        this.userRepository = userRepository;
        this.refreshTokenFactory = refreshTokenFactory;
        this.localAccountEmailService = localAccountEmailService;
        this.authenticationSettings = authenticationSettings;
    }

    public async Task<ApplicationResult> HandleAsync(ForgotPasswordCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        string? normalizedEmail = UserRules.NormalizeEmail(command.Request.Email);
        if (!UserRules.IsValidEmail(normalizedEmail))
        {
            return ApplicationResult.Failure(UserApplicationErrors.InvalidEmailAddress());
        }

        User? user = await this.userRepository.GetByEmailAsync(normalizedEmail!, cancellationToken);
        if (user is not null && user.IsActivated && !string.IsNullOrWhiteSpace(user.HashedPassword))
        {
            DateTime now = DateTime.UtcNow;
            string resetToken = this.refreshTokenFactory.Generate();
            user.PasswordResetTokenHash = this.refreshTokenFactory.ComputeHash(resetToken);
            user.PasswordResetTokenExpiresAtUtc = now.AddMinutes(this.authenticationSettings.PasswordResetTokenExpirationMinutes);
            user.PasswordResetSentAtUtc = now;
            user.UpdatedAtUtc = now;

            User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
            if (updatedUser is null)
            {
                return ApplicationResult.Failure(UserApplicationErrors.PasswordResetEmailSendFailed());
            }

            await this.localAccountEmailService.SendPasswordResetAsync(updatedUser, resetToken, cancellationToken);
        }

        return ApplicationResult.Success();
    }
}
