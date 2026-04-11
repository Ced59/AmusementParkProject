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
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IRefreshTokenRepository refreshTokenRepository;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        ITokenService tokenService,
        IUserAuthenticationSettings authenticationSettings,
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokenRepository)
    {
        this.userRepository = userRepository;
        this.tokenService = tokenService;
        this.authenticationSettings = authenticationSettings;
        this.refreshTokenFactory = refreshTokenFactory;
        this.refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ApplicationResult<AuthenticatedUserResult>> HandleAsync(RefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (command.Request is null || string.IsNullOrWhiteSpace(command.Request.RefreshToken))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        string tokenHash = this.refreshTokenFactory.ComputeHash(command.Request.RefreshToken);
        RefreshToken? currentRefreshToken = await this.refreshTokenRepository.GetByTokenHashAsync(tokenHash, cancellationToken);
        if (currentRefreshToken is null || !currentRefreshToken.IsActiveAt(DateTime.UtcNow))
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        User? user = await this.userRepository.GetByIdAsync(currentRefreshToken.UserId, cancellationToken);
        if (user is null || user.IsBlocked || !user.IsActivated)
        {
            await this.refreshTokenRepository.RevokeAsync(tokenHash, "UserInvalidated", cancellationToken);
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        DateTime now = DateTime.UtcNow;
        user.LastActivityUtc = now;

        string accessToken = this.tokenService.GenerateUserToken(user);
        string replacementRawToken = this.refreshTokenFactory.Generate();
        string replacementHash = this.refreshTokenFactory.ComputeHash(replacementRawToken);
        DateTime replacementExpiresAtUtc = now.AddMinutes(this.authenticationSettings.TokenRefreshLimitMinutes);

        bool rotated = await this.refreshTokenRepository.RotateAsync(
            tokenHash,
            new RefreshToken
            {
                UserId = user.Id,
                TokenHash = replacementHash,
                ExpiresAtUtc = replacementExpiresAtUtc,
                CreatedAtUtc = now,
                UpdatedAtUtc = now,
            },
            cancellationToken);

        if (!rotated)
        {
            return ApplicationResult<AuthenticatedUserResult>.Failure(UserApplicationErrors.TokenRefreshFailed());
        }

        await this.userRepository.UpdateLastActivityAsync(user.Id, cancellationToken);

        return ApplicationResult<AuthenticatedUserResult>.Success(new AuthenticatedUserResult
        {
            User = user,
            AccessToken = accessToken,
            RefreshToken = replacementRawToken,
            RefreshTokenExpiresAtUtc = replacementExpiresAtUtc,
        });
    }
}
