using Dtos.Pagination;
using Dtos.Searching;
using Entities.Model.Errors;
using Microsoft.AspNetCore.Mvc;
using OneOf;
using Services.Interfaces.Searching;
using WebAPI.ResponseHandlers;
using WebAPI.Settings.Attributes;

namespace WebAPI.Controllers
{
    [ApiController]
    [SwaggerOrder(4)]
    [Route("[controller]")]
    public class SearchController : ControllerBase
    {
        private readonly ISearchService searchService;

        public SearchController(ISearchService searchService)
        {
            this.searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] string[] categories,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            string[] normalizedCategories = (categories ?? Array.Empty<string>())
                .SelectMany(category => category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (string.IsNullOrWhiteSpace(query) && normalizedCategories.Length == 0)
            {
                return BadRequest("Vous devez fournir un terme de recherche ou au moins une catégorie.");
            }

            OneOf<(IEnumerable<SearchResultDto> Data, PaginationDto Pagination), ErrorCodes.ErrorDetail> result = await searchService.SearchAsync(query, normalizedCategories, page, pageSize);

            return result.Match(
                success => ApiResponseHandler.HandleResponse(success.Data, success.Pagination),
                ApiResponseHandler.HandleResponse);
        }
    }
}
