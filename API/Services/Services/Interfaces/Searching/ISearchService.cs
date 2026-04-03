using System.Collections.Generic;
using System.Threading.Tasks;
using Dtos.Pagination;
using Dtos.Searching;
using OneOf;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Interfaces.Searching
{
    public interface ISearchService
    {
        /// <summary>
        /// Recherche full-text + filtre catégories + pagination dans la collection SearchItems.
        /// En cas de succès, renvoie une paire :
        ///   - IEnumerable de SearchResultDto : la page courante de résultats
        ///   - PaginationDto           : infos de pagination (totalItems, totalPages, etc.)
        /// Sinon, renvoie un ErrorDetail.
        /// </summary>
        Task<OneOf<(IEnumerable<SearchResultDto> Data, PaginationDto Pagination), ErrorDetail>>
            SearchAsync(string query, string[] categories, int page, int pageSize);
    }
}