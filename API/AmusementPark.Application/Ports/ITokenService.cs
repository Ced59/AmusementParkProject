using AmusementPark.Core.Domain.Users;

namespace AmusementPark.Application.Ports
{
    /// <summary>
    /// Port applicatif de génération et de lecture de tokens JWT.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Génère un token JWT pour un utilisateur applicatif.
        /// </summary>
        string GenerateUserToken(User user);

        /// <summary>
        /// Valide un token JWT et retourne ses informations utiles.
        /// </summary>
        TokenValidationResult ValidateToken(string token, bool validateLifetime);
    }
}
