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

    public LoginCommandHandler(IUserRepository userRepository, IPasswordHasher passwordHasher, ITokenService tokenService)
    {
        this.userRepository = userRepository;
        this.passwordHasher = passwordHasher;
        this.tokenService = tokenService;
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

        string token = this.tokenService.GenerateUserToken(user);
        await this.userRepository.UpdateLastLoginAndActivityAsync(user.Id, cancellationToken);

        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = user,
            AccessToken = token,
            RefreshToken = token,
        });
    }
}
