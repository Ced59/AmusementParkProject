using System.Security.Claims;

namespace AmusementPark.Application.Ports;

/// <summary>
/// Résultat transport-agnostique de validation d'un token.
/// </summary>
public sealed class TokenValidationResult
{
    public bool IsValid { get; init; }

    public string? Subject { get; init; }

    public IReadOnlyCollection<Claim> Claims { get; init; } = Array.Empty<Claim>();
}
