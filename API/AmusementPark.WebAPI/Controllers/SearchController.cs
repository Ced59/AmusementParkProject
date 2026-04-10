using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Requests;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Search.Queries;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Searching;
using AmusementPark.WebAPI.Mappers;
using AmusementPark.WebAPI.Responses;
using Microsoft.AspNetCore.Mvc;

namespace AmusementPark.WebAPI.Controllers;

/// <summary>
/// Contrôleur Clean Architecture de la recherche transverse.
/// </summary>
[ApiController]
[Route("[controller]")]
public sealed class SearchController : ControllerBase
{
    private readonly IQueryHandler<SearchQuery, ApplicationResult<SearchResultPage<SearchHitResult>>> searchQueryHandler;

    public SearchController(IQueryHandler<SearchQuery, ApplicationResult<SearchResultPage<SearchHitResult>>> searchQueryHandler)
    {
        this.searchQueryHandler = searchQueryHandler;
    }

    [HttpGet]
    [ProducesResponseType(typeof(PagedResponseDto<SearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? query,
        [FromQuery] string[]? categories,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        string[] normalizedCategories = (categories ?? Array.Empty<string>())
            .SelectMany(static category => category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.IsNullOrWhiteSpace(query) && normalizedCategories.Length == 0)
        {
            return this.BadRequest("Vous devez fournir un terme de recherche ou au moins une catégorie.");
        }

        ApplicationResult<SearchResultPage<SearchHitResult>> result = await this.searchQueryHandler.HandleAsync(
            new SearchQuery(query ?? string.Empty, normalizedCategories, new PagedQuery(page, pageSize)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToHttp());
    }
}
