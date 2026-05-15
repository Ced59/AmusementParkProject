using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de mise à jour du profil utilisateur.
/// </summary>
public sealed class UpdateUserProfileCommandHandler : ICommandHandler<UpdateUserProfileCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly ILocalAccountEmailService localAccountEmailService;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public UpdateUserProfileCommandHandler(
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

    public async Task<ApplicationResult<User>> HandleAsync(UpdateUserProfileCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.UserId))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        if (command.Update is null)
        {
            return ApplicationResult<User>.Failure(ApplicationErrors.Required(nameof(command.Update)));
        }

        string? currentEmail = UserRules.NormalizeEmail(command.Update.Email);
        string? newEmail = UserRules.NormalizeEmail(command.Update.NewEmail);

        if (!UserRules.IsValidEmail(currentEmail) || (newEmail is not null && !UserRules.IsValidEmail(newEmail)))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.InvalidEmailAddress());
        }

        User? user = await this.userRepository.GetByIdAsync(command.UserId.Trim(), cancellationToken);
        if (user is null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserNotExists());
        }

        string? confirmationToken = null;
        bool emailChanged = !string.IsNullOrWhiteSpace(newEmail) && !string.Equals(user.Email, newEmail, StringComparison.OrdinalIgnoreCase);
        if (emailChanged)
        {
            User? existingUser = await this.userRepository.GetByEmailAsync(newEmail!, cancellationToken);
            if (existingUser is not null && !string.Equals(existingUser.Id, user.Id, StringComparison.Ordinal))
            {
                return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
            }

            DateTime now = DateTime.UtcNow;
            confirmationToken = this.refreshTokenFactory.Generate();
            user.Email = newEmail;
            user.IsActivated = false;
            user.EmailConfirmationTokenHash = this.refreshTokenFactory.ComputeHash(confirmationToken);
            user.EmailConfirmationTokenExpiresAtUtc = now.AddHours(this.authenticationSettings.EmailConfirmationTokenExpirationHours);
            user.EmailConfirmationSentAtUtc = now;
        }

        user.FirstName = command.Update.FirstName;
        user.LastName = command.Update.LastName;
        user.PreferredLanguage = command.Update.PreferredLanguage;
        user.LastActivityUtc = DateTime.UtcNow;
        user.UpdatedAtUtc = DateTime.UtcNow;
        if (command.Update.AvatarUrl is not null)
        {
            user.AvatarUrl = command.Update.AvatarUrl;
        }

        try
        {
            User? updatedUser = await this.userRepository.UpdateAsync(user.Id, user, cancellationToken);
            if (updatedUser is null)
            {
                return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
            }

            if (!string.IsNullOrWhiteSpace(confirmationToken))
            {
                await this.localAccountEmailService.SendEmailConfirmationAsync(updatedUser, confirmationToken, cancellationToken);
            }

            return ApplicationResult<User>.Success(updatedUser);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserUpdateFailed());
        }
    }
}
