using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
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
using AmusementPark.WebAPI.OutputCaching;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Net.Http.Headers;

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
    [OutputCache(PolicyName = ApiOutputCachePolicyNames.PublicDataShort)]
    [AllowAnonymous]
    [ProducesResponseType(typeof(PagedResponseDto<SearchResultDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> SearchAsync(
        [FromQuery] string? query,
        [FromQuery] string[]? categories,
        [FromQuery] PaginationRequestDto pagination,
        [FromQuery(Name = "pageSize")] int? legacyPageSize = null,
        CancellationToken cancellationToken = default)
    {
        string[] normalizedCategories = (categories ?? Array.Empty<string>())
            .SelectMany(static category => category.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (string.IsNullOrWhiteSpace(query) && normalizedCategories.Length == 0)
        {
            return this.ToProblemDetailsResult(StatusCodes.Status400BadRequest, "Vous devez fournir un terme de recherche ou au moins une catégorie.", "search.query-or-category-required");
        }

        PaginationRequestDto effectivePagination = pagination.Override(size: legacyPageSize);

        ApplicationResult<SearchResultPage<SearchHitResult>> result = await this.searchQueryHandler.HandleAsync(
            new SearchQuery(query ?? string.Empty, normalizedCategories, effectivePagination.ToApplication(), ResolveRequestLanguageCode(this.Request)),
            cancellationToken);

        if (!result.IsSuccess || result.Value is null)
        {
            return this.ToActionResult(result);
        }

        return this.Ok(result.Value.ToPagedResponse(static item => item.ToHttp()));
    }

    private static string ResolveRequestLanguageCode(HttpRequest request)
    {
        string acceptLanguage = request.Headers[HeaderNames.AcceptLanguage].ToString();
        if (string.IsNullOrWhiteSpace(acceptLanguage))
        {
            return "en";
        }

        string firstLanguage = acceptLanguage.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;
        string languageCode = firstLanguage.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .FirstOrDefault() ?? string.Empty;

        if (languageCode.Length < 2)
        {
            return "en";
        }

        string normalizedLanguageCode = languageCode.Trim().ToLowerInvariant();
        int separatorIndex = normalizedLanguageCode.IndexOf('-', StringComparison.Ordinal);
        if (separatorIndex <= 0)
        {
            separatorIndex = normalizedLanguageCode.IndexOf('_', StringComparison.Ordinal);
        }

        return separatorIndex > 0
            ? normalizedLanguageCode[..separatorIndex]
            : normalizedLanguageCode;
    }
}
