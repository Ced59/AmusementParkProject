namespace AmusementPark.Application.Ports
{
    /// <summary>
    /// Port applicatif de génération et de lecture de tokens.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>
        /// Génère un token applicatif à partir d'un sujet et de claims.
        /// </summary>
        string GenerateToken(string subject, IReadOnlyDictionary<string, string> claims);
    }
}
