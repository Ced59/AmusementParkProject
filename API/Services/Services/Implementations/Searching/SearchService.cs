using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dtos.Pagination;
using Dtos.Searching;
using Entities.Model.Errors;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Searching;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Searching
{
    public class SearchService : ISearchService
    {
        private ISearchQueryHandler _queryHandler;

        public SearchService(ISearchQueryHandler queryHandler)
        {
            _queryHandler = queryHandler;
        }

        public async Task<OneOf<(IEnumerable<SearchResultDto> Data, PaginationDto Pagination), ErrorDetail>>
            SearchAsync(string query, string[] categories, int page, int pageSize)
        {
            // 1) Valider page / pageSize
            if (page <= 0 || pageSize <= 0)
            {
                return new ErrorDetail(400, "page et pageSize doivent être supérieurs à zéro.");
            }

            try
            {
                // 2) Interroger le query handler
                (IEnumerable<SearchItem>? searchItems, var totalCount) = await _queryHandler.SearchAsync(query, categories, page, pageSize);

                // 3) Si aucun résultat trouvé
                if (totalCount == 0 || searchItems == null || !searchItems.Any())
                {
                    return new ErrorDetail(404, "Aucun résultat pour cette recherche.");
                }

                // 4) Calculer la pagination
                PaginationDto paginationInfo = PaginationDto.Create(
                    Convert.ToInt32(totalCount),
                    page,
                    pageSize);

                // 5) Mapper SearchItem -> SearchResultDto pour chaque élément de la page
                List<SearchResultDto>? resultDtos = searchItems.Select(item => new SearchResultDto
                {
                    Title = item.Title,
                    Description = item.Description
                }).ToList();

                // 6) Retourner la paire (Data, Pagination)
                return (resultDtos, paginationInfo);
            }
            catch (Exception ex)
            {
                // 7) En cas d’exception inattendue, renvoyer 500
                return new ErrorDetail(500, $"Erreur interne lors de la recherche : {ex.Message}");
            }
        }
    }
}