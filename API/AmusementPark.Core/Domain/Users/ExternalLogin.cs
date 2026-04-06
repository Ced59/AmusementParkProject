namespace AmusementPark.Core.Domain.Users;

/// <summary>
/// Lien vers un compte d'authentification externe.
/// </summary>
public sealed class ExternalLogin
{
    public ExternalLoginProvider Provider { get; set; }

    public string ProviderUserId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string? DisplayName { get; set; }

    public string? GivenName { get; set; }

    public string? FamilyName { get; set; }

    public string? PictureUrl { get; set; }

    public string? HostedDomain { get; set; }

    public DateTime LinkedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime LastLoginAtUtc { get; set; } = DateTime.UtcNow;
}
