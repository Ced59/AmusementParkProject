using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Représente un refresh token opaque persistant, traçable et révocable.
/// </summary>
public sealed class RefreshToken : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? ReplacedByTokenHash { get; set; }

    public string? RevocationReason { get; set; }

    public bool IsActiveAt(DateTime utcNow)
    {
        return !string.IsNullOrWhiteSpace(TokenHash)
               && RevokedAtUtc is null
               && ExpiresAtUtc > utcNow;
    }
}
