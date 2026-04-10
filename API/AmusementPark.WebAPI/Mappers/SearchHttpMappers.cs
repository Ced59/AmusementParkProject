using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.WebAPI.Contracts.Common;
using AmusementPark.WebAPI.Contracts.Searching;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour la feature Search.
/// </summary>
internal static class SearchHttpMappers
{
    public static PagedResponseDto<SearchResultDto> ToHttp(this SearchResultPage<SearchHitResult> page)
    {
        ArgumentNullException.ThrowIfNull(page);

        return new PagedResponseDto<SearchResultDto>
        {
            Data = page.Items.Select(ToHttp).ToList(),
            Pagination = new PaginationDto
            {
                TotalItems = (int)page.TotalItems,
                TotalPages = page.TotalPages,
                CurrentPage = page.Page,
                ItemsPerPage = page.PageSize,
            },
        };
    }

    public static SearchResultDto ToHttp(this SearchHitResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return new SearchResultDto
        {
            OriginalId = result.Id,
            Category = !string.IsNullOrWhiteSpace(result.Category) ? result.Category : result.ResourceType,
            Title = result.Title,
            Description = result.Description ?? string.Empty,
        };
    }
}
