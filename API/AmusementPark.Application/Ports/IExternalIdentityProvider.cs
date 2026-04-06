namespace AmusementPark.Application.Ports
{
    /// <summary>
    /// Port applicatif pour un fournisseur d'identité externe.
    /// </summary>
    public interface IExternalIdentityProvider
    {
        /// <summary>
        /// Valide un jeton externe et retourne un sujet stable.
        /// </summary>
        Task<string?> ValidateAsync(string providerToken, CancellationToken cancellationToken);
    }
}
