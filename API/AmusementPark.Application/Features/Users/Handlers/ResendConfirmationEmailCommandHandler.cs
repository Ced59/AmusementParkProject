using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de renvoi de confirmation email.
/// </summary>
public sealed class ResendConfirmationEmailCommandHandler : ICommandHandler<ResendConfirmationEmailCommand, ApplicationResult>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly ILocalAccountEmailService localAccountEmailService;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public ResendConfirmationEmailCommandHandler(
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

    public async Task<ApplicationResult> HandleAsync(ResendConfirmationEmailCommand command, CancellationToken cancellationToken = default)
    {
        string? normalizedEmail = UserRules.NormalizeEmail(command.Email);
        if (!UserRules.IsValidEmail(normalizedEmail))
        {
            return ApplicationResult.Failure(UserApplicationErrors.InvalidEmailAddress());
        }

        User? user = await this.userRepository.GetByEmailAsync(normalizedEmail!, cancellationToken);
        if (user is not null && !user.IsActivated)
        {
            DateTime now = DateTime.UtcNow;
            string confirmationToken = this.refreshTokenFactory.Generate();
            user.EmailConfirmationTokenHash = this.refreshTokenFactory.ComputeHash(confirmationToken);
            user.EmailConfirmationTokenExpiresAtUtc = now.AddHours(this.authenticationSettings.EmailConfirmationTokenExpirationHours);
            user.EmailConfirmationSentAtUtc = now;
            user.UpdatedAtUtc = now;

            User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
            if (updatedUser is null)
            {
                return ApplicationResult.Failure(UserApplicationErrors.ConfirmationEmailResendFailed());
            }

            await this.localAccountEmailService.SendEmailConfirmationAsync(updatedUser, confirmationToken, cancellationToken);
        }

        return ApplicationResult.Success();
    }
}
