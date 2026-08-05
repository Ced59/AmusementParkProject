using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Features.ParkDataEditorTokens.Ports;

public interface IParkDataEditorAccessTokenRepository
{
    Task<ParkDataEditorAccessToken?> GetByIdAsync(string tokenId, CancellationToken cancellationToken);

    Task<IReadOnlyCollection<ParkDataEditorAccessToken>> GetByUserIdAsync(
        string userId,
        CancellationToken cancellationToken);

    Task<long> CountActiveByUserIdAsync(
        string userId,
        DateTime utcNow,
        CancellationToken cancellationToken);

    Task CreateAsync(ParkDataEditorAccessToken token, CancellationToken cancellationToken);

    Task<bool> MarkUsedAsync(
        string tokenId,
        DateTime usedAtUtc,
        DateTime updateOnlyIfLastUsedBeforeUtc,
        CancellationToken cancellationToken);

    Task<long> RevokeAsync(
        string userId,
        string? tokenId,
        string revokedByUserId,
        string reason,
        DateTime revokedAtUtc,
        CancellationToken cancellationToken);
}
