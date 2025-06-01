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
        private readonly ISearchService _searchService;

        public SearchController(ISearchService searchService)
        {
            _searchService = searchService;
        }

        [HttpGet]
        public async Task<IActionResult> Search(
            [FromQuery] string query,
            [FromQuery] string[] categories,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (string.IsNullOrWhiteSpace(query) && (categories == null || categories.Length == 0))
                return BadRequest("Vous devez fournir un terme de recherche ou au moins une catégorie.");

            OneOf<SearchResultDto, ErrorCodes.ErrorDetail> result = await _searchService.SearchAsync(query, categories, page, pageSize);
            return ApiResponseHandler.HandleResponse(result);
        }
    }
}