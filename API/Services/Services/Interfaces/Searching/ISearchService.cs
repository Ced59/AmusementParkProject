using Dtos.Searching;
using Entities.Model.Errors;
using OneOf;

namespace Services.Interfaces.Searching
{
    public interface ISearchService
    {
        Task<OneOf<SearchResultDto, ErrorCodes.ErrorDetail>> SearchAsync(string query, string[] categories, int page, int pageSize);
    }
}