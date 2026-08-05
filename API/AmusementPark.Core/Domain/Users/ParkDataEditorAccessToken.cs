using AmusementPark.Core.Abstractions;

namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Jeton d'accès opaque, limité aux opérations d'intégration des données de parcs.
/// Le secret en clair n'est jamais conservé dans le domaine persistant.
/// </summary>
public sealed class ParkDataEditorAccessToken : AuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string Label { get; set; } = string.Empty;

    public string TokenHash { get; set; } = string.Empty;

    public string DisplayPrefix { get; set; } = string.Empty;

    public DateTime ExpiresAtUtc { get; set; }

    public DateTime? LastUsedAtUtc { get; set; }

    public DateTime? RevokedAtUtc { get; set; }

    public string? RevokedByUserId { get; set; }

    public string? RevocationReason { get; set; }

    public bool IsActiveAt(DateTime utcNow)
    {
        return !string.IsNullOrWhiteSpace(this.TokenHash)
               && this.RevokedAtUtc is null
               && this.ExpiresAtUtc > utcNow;
    }
}
