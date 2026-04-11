using System;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.WebAPI.Contracts.Searching;

namespace AmusementPark.WebAPI.Mappers;

/// <summary>
/// Helpers de mapping HTTP pour la feature Search.
/// </summary>
internal static class SearchHttpMappers
{
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
