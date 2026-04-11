using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.Users.Ports;

/// <summary>
/// Port applicatif de persistance des refresh tokens.
/// </summary>
public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken);

    Task CreateAsync(RefreshToken refreshToken, CancellationToken cancellationToken);

    Task<bool> RotateAsync(string currentTokenHash, RefreshToken replacementToken, CancellationToken cancellationToken);

    Task<bool> RevokeAsync(string tokenHash, string reason, CancellationToken cancellationToken);
}
