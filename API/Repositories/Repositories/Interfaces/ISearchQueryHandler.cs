using Entities.Model.Searching;

namespace Repositories.Interfaces
{
    public interface ISearchQueryHandler
    {
        Task<(IEnumerable<SearchItem> Items, long TotalCount)> SearchAsync(string? query, string[]? categories,
            int page, int pageSize);
    }
}