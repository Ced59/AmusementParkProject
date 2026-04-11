namespace AmusementPark.Application.Features.Search.Ports
{
    /// <summary>
    /// Port applicatif de projection technique pour la recherche.
    /// </summary>
    public interface ISearchProjectionWriter
    {
        /// <summary>
        /// Met à jour la projection de recherche d'une ressource.
        /// </summary>
        Task UpsertAsync(string resourceType, string resourceId, CancellationToken cancellationToken);

        /// <summary>
        /// Met à jour la projection de recherche d'un ensemble de ressources.
        /// </summary>
        Task UpsertManyAsync(string resourceType, IReadOnlyCollection<string> resourceIds, CancellationToken cancellationToken);

        /// <summary>
        /// Supprime une ressource de la projection de recherche.
        /// </summary>
        Task DeleteAsync(string resourceType, string resourceId, CancellationToken cancellationToken);
    }
}
