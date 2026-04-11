using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Users.Commands;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Ports;

namespace AmusementPark.Application.Features.Users.Handlers;

/// <summary>
/// Handler de révocation d'un refresh token.
/// </summary>
public sealed class RevokeRefreshTokenCommandHandler : ICommandHandler<RevokeRefreshTokenCommand, ApplicationResult>
{
    private readonly IRefreshTokenFactory refreshTokenFactory;
    private readonly IRefreshTokenRepository refreshTokenRepository;

    public RevokeRefreshTokenCommandHandler(
        IRefreshTokenFactory refreshTokenFactory,
        IRefreshTokenRepository refreshTokenRepository)
    {
        this.refreshTokenFactory = refreshTokenFactory;
        this.refreshTokenRepository = refreshTokenRepository;
    }

    public async Task<ApplicationResult> HandleAsync(RevokeRefreshTokenCommand command, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(command.RefreshToken))
        {
            return ApplicationResult.Success();
        }

        string tokenHash = this.refreshTokenFactory.ComputeHash(command.RefreshToken);
        string reason = string.IsNullOrWhiteSpace(command.Reason) ? "Revoked" : command.Reason;

        await this.refreshTokenRepository.RevokeAsync(tokenHash, reason, cancellationToken);
        return ApplicationResult.Success();
    }
}
