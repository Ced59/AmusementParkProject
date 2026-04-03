using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Dtos.Pagination;
using Dtos.Searching;
using Entities.Model.Searching;
using OneOf;
using Repositories.Interfaces;
using Services.Interfaces.Searching;
using static Entities.Model.Errors.ErrorCodes;

namespace Services.Implementations.Searching
{
    public class SearchService : ISearchService
    {
        private readonly ISearchQueryHandler queryHandler;

        public SearchService(ISearchQueryHandler queryHandler)
        {
            this.queryHandler = queryHandler;
        }

        public async Task<OneOf<(IEnumerable<SearchResultDto> Data, PaginationDto Pagination), ErrorDetail>>
            SearchAsync(string query, string[] categories, int page, int pageSize)
        {
            if (page <= 0 || pageSize <= 0)
            {
                return new ErrorDetail(400, "page et pageSize doivent être supérieurs à zéro.");
            }

            try
            {
                (IEnumerable<SearchItem> searchItems, long totalCount) = await queryHandler.SearchAsync(query, categories, page, pageSize);

                if (totalCount == 0 || searchItems == null || !searchItems.Any())
                {
                    return new ErrorDetail(404, "Aucun résultat pour cette recherche.");
                }

                PaginationDto paginationInfo = PaginationDto.Create(Convert.ToInt32(totalCount), page, pageSize);

                List<SearchResultDto> resultDtos = searchItems.Select(item => new SearchResultDto
                {
                    OriginalId = item.OriginalId,
                    Category = item.Category,
                    Title = item.Title,
                    Description = item.Description
                }).ToList();

                return (resultDtos, paginationInfo);
            }
            catch (Exception ex)
            {
                return new ErrorDetail(500, $"Erreur interne lors de la recherche : {ex.Message}");
            }
        }
    }
}
