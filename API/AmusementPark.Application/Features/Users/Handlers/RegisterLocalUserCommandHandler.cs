using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'inscription d'un utilisateur local.
/// </summary>
public sealed class RegisterLocalUserCommandHandler : ICommandHandler<RegisterLocalUserCommand, ApplicationResult<User>>
{
    private readonly IUserRepository userRepository;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IPasswordHasher passwordHasher;
    private readonly ILocalAccountEmailService localAccountEmailService;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public RegisterLocalUserCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenFactory refreshTokenFactory,
        IPasswordHasher passwordHasher,
        ILocalAccountEmailService localAccountEmailService,
        IUserAuthenticationSettings authenticationSettings)
    {
        this.userRepository = userRepository;
        this.refreshTokenFactory = refreshTokenFactory;
        this.passwordHasher = passwordHasher;
        this.localAccountEmailService = localAccountEmailService;
        this.authenticationSettings = authenticationSettings;
    }

    public async Task<ApplicationResult<User>> HandleAsync(RegisterLocalUserCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<User>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        if (!string.Equals(command.Request.Password, command.Request.VerifyPassword, StringComparison.Ordinal))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.PasswordsNotSames());
        }

        string? normalizedEmail = UserRules.NormalizeEmail(command.Request.Email);
        if (!UserRules.IsValidEmail(normalizedEmail))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.InvalidEmailAddress());
        }

        if (!UserRules.IsValidPassword(command.Request.Password))
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.InvalidPassword());
        }

        User? existingUser = await this.userRepository.GetByEmailAsync(normalizedEmail!, cancellationToken);
        if (existingUser is not null)
        {
            return ApplicationResult<User>.Failure(UserApplicationErrors.UserAlreadyExists());
        }

        try
        {
            DateTime now = DateTime.UtcNow;
            string confirmationToken = this.refreshTokenFactory.Generate();

            User user = new User
            {
                Email = normalizedEmail,
                PreferredLanguage = UserRules.NormalizePreferredLanguage(command.Request.PreferredLanguage),
                PreferredMeasurementSystem = UserRules.NormalizePreferredMeasurementSystem(command.Request.PreferredMeasurementSystem),
                IsActivated = false,
                IsBlocked = false,
                Roles = new List<Role> { Role.User },
                HashedPassword = this.passwordHasher.HashPassword(command.Request.Password!),
                LastLoginUtc = now,
                LastActivityUtc = now,
                EmailConfirmationTokenHash = this.refreshTokenFactory.ComputeHash(confirmationToken),
                EmailConfirmationTokenExpiresAtUtc = now.AddHours(this.authenticationSettings.EmailConfirmationTokenExpirationHours),
                EmailConfirmationSentAtUtc = now,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            };

            User createdUser = await this.userRepository.CreateAsync(user, cancellationToken);
            await this.localAccountEmailService.SendEmailConfirmationAsync(createdUser, confirmationToken, cancellationToken);
            return ApplicationResult<User>.Success(createdUser);
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
