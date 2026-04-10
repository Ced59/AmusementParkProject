using System.Text.RegularExpressions;
using Entities.Model.Searching;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class SearchMongoQueryHandler : ISearchQueryHandler
    {
        private readonly IMongoCollection<SearchItem> searchCollection;

        public SearchMongoQueryHandler(
            IMongoDatabase database,
            IMongoDbSettings settings)
        {
            searchCollection = database.GetCollection<SearchItem>(settings.SearchItemCollectionName);
        }

        public async Task<(IEnumerable<SearchItem> Items, long TotalCount)> SearchAsync(
            string? query,
            string[]? categories,
            int page,
            int pageSize)
        {
            FilterDefinition<SearchItem> filter = Builders<SearchItem>.Filter.Empty;

            if (!string.IsNullOrWhiteSpace(query))
            {
                string escapedQuery = Regex.Escape(query.Trim());
                BsonRegularExpression regex = new($".*{escapedQuery}.*", "i");

                FilterDefinition<SearchItem> queryFilter = Builders<SearchItem>.Filter.Or(
                    Builders<SearchItem>.Filter.Regex(searchItem => searchItem.Title, regex),
                    Builders<SearchItem>.Filter.Regex(searchItem => searchItem.Description, regex),
                    Builders<SearchItem>.Filter.Regex("keywords", regex));

                filter &= queryFilter;
            }

            if (categories != null && categories.Length > 0)
            {
                string[] normalizedCategories = categories
                    .Where(category => !string.IsNullOrWhiteSpace(category))
                    .Select(category => category.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (normalizedCategories.Length > 0)
                {
                    FilterDefinition<SearchItem> categoryFilter = Builders<SearchItem>.Filter.In(searchItem => searchItem.Category, normalizedCategories);
                    filter &= categoryFilter;
                }
            }

            filter &= Builders<SearchItem>.Filter.Eq(searchItem => searchItem.IsVisible, true);

            long totalCount = await searchCollection.CountDocumentsAsync(filter);

            SortDefinition<SearchItem> sort = Builders<SearchItem>.Sort.Descending(searchItem => searchItem.UpdatedAt);
            FindOptions<SearchItem> findOptions = new()
            {
                Sort = sort,
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            };

            IAsyncCursor<SearchItem> cursor = await searchCollection.FindAsync(filter, findOptions);
            List<SearchItem> items = await cursor.ToListAsync();

            return (items, totalCount);
        }
    }
}
