using System.Security.Claims;
using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Users.Results;
using AmusementPark.Application.Ports;
using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de rafraîchissement de token.
/// </summary>
public sealed class RefreshTokenCommandHandler : ICommandHandler<RefreshTokenCommand, ApplicationResult<AuthenticatedUserResult>>
{
    private readonly IUserRepository userRepository;
    private readonly ITokenService tokenService;
    private readonly IUserAuthenticationSettings authenticationSettings;

    public RefreshTokenCommandHandler(IUserRepository userRepository, ITokenService tokenService, IUserAuthenticationSettings authenticationSettings)
    {
        this.userRepository = userRepository;
        this.tokenService = tokenService;
        this.authenticationSettings = authenticationSettings;
    }

    public async Task<ApplicationResult<AuthenticatedUserResult>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.RefreshToken))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        AmusementPark.Application.Ports.TokenValidationResult validation = this.tokenService.ValidateToken(command.Request.RefreshToken, false);
        if (!validation.IsValid)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        string? userId = validation.Claims.FirstOrDefault(static claim => claim.Type == ClaimTypes.NameIdentifier)?.Value ?? validation.Subject;
        if (string.IsNullOrWhiteSpace(userId))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        User? user = await this.userRepository.GetByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        DateTime expireAt = user.LastActivityUtc.AddMinutes(this.authenticationSettings.TokenRefreshLimitMinutes);
        if (DateTime.UtcNow > expireAt)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailedInactivity());
        }

        string token = this.tokenService.GenerateUserToken(user);
        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = user,
            AccessToken = token,
            RefreshToken = token,
        });
    }
}
