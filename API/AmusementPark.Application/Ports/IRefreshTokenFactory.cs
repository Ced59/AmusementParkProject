namespace AmusementPark.Application.Ports;

/// <summary>
/// Fabrique applicative de refresh tokens.
/// </summary>
public interface IRefreshTokenFactory
{
    /// <summary>
    /// Génère une valeur opaque de refresh token.
    /// </summary>
    string Generate();
}
