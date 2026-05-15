using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler d'authentification locale.
/// </summary>
public sealed class LoginCommandHandler : ICommandHandler<LoginCommand, ApplicationResult<AuthenticatedUserResult>>
{
    private readonly IUserRepository userRepository;
    private readonly IPasswordHasher passwordHasher;
    private readonly ITokenService tokenService;
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IRefreshTokenRepository refreshTokenRepository;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        ITokenService tokenService,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokenRepository,
        IUserAuthenticationSettings authenticationSettings)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
        this.refreshTokenFactory = refreshTokenFactory;
        this.refreshTokenRepository = refreshTokenRepository;
        this.authenticationSettings = authenticationSettings;
    }

    public async Task<ApplicationResult<AuthenticatedUserResult>> HandleAsync(LoginCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(ApplicationErrors.Required(nameof(command.Request)));
        }

        string? normalizedEmail = UserRules.NormalizeEmail(command.Request.Email);
        if (string.IsNullOrWhiteSpace(normalizedEmail))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LoginFailed());
        }

        User? user = await this.userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LoginFailed());
        }

        if (string.IsNullOrWhiteSpace(user.HashedPassword))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LocalLoginNotAvailable());
        }

        if (!this.passwordHasher.VerifyPassword(command.Request.Password ?? string.Empty, user.HashedPassword))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.LoginFailed());
        }

        if (!user.IsActivated)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserNotActivated());
        }

        if (user.IsBlocked)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.UserBlocked());
        }

        DateTime now = DateTime.UtcNow;
        user.LastLoginUtc = now;
        user.LastActivityUtc = now;

        string accessToken = this.tokenService.GenerateUserToken(user);
        string refreshToken = this.refreshTokenFactory.Generate();
        DateTime refreshTokenExpiresAtUtc = now.AddMinutes(this.authenticationSettings.TokenRefreshLimitMinutes);

        await this.refreshTokenRepository.CreateAsync(
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = this.refreshTokenFactory.ComputeHash(refreshToken),
                ExpiresAtUtc = refreshTokenExpiresAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            cancellationToken);

        await this.userRepository.UpdateLastLoginAndActivityAsync(user.Id, cancellationToken);

        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = user,
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            RefreshTokenExpiresAtUtc = refreshTokenExpiresAtUtc,
        });
    }
}
