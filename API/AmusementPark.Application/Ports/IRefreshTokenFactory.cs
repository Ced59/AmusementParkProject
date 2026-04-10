namespace AmusementPark.Application.Ports;

/// <summary>
/// Fabrique applicative de tokens opaques utilisés pour les flux locaux (confirmation email, reset password).
/// </summary>
public interface IRefreshTokenFactory
{
    /// <summary>
    /// Génère une valeur opaque de token.
    /// </summary>
    string Generate();

    /// <summary>
    /// Calcule l'empreinte persistable d'un token opaque.
    /// </summary>
    string ComputeHash(string token);
}
