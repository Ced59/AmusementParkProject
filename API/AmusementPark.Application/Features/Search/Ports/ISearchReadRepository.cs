using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Search.Results;

namespace AmusementPark.Application.Features.Search.Ports;

/// <summary>
/// Port applicatif de lecture pour la recherche.
/// </summary>
public interface ISearchReadRepository
{
    /// <summary>
    /// Exécute une recherche paginée.
    /// </summary>
    Task<SearchResultPage<SearchHitResult>> SearchAsync(string text, IReadOnlyCollection<string> categories, int page, int pageSize, CancellationToken cancellationToken);
}
