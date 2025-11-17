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
            searchCollection = database
                .GetCollection<SearchItem>(settings.SearchItemCollectionName);
        }

        public async Task<(IEnumerable<SearchItem> Items, long TotalCount)> SearchAsync(
            string? query,
            string[]? categories,
            int page,
            int pageSize)
        {
            // 1) Filtre initial “match all”
            FilterDefinition<SearchItem> filter =
                Builders<SearchItem>.Filter.Empty;

            // 2) Filtre texte
            if (!string.IsNullOrWhiteSpace(query))
            {
                string escaped = Regex.Escape(query.Trim());
                BsonRegularExpression regex = new($".*{escaped}.*", "i");

                filter = Builders<SearchItem>
                    .Filter.Regex(si => si.Title, regex);
            }

            // 3) Filtre catégories
            if (categories != null && categories.Length > 0)
            {
                FilterDefinition<SearchItem> catFilter =
                    Builders<SearchItem>
                        .Filter.In(si => si.Category, categories);

                filter = filter & catFilter;
            }

            // 4) 🔹 Filtre visibilité publique : uniquement les items visibles
            filter = filter & Builders<SearchItem>
                .Filter.Eq(si => si.IsVisible, true);

            // 5) Count total
            long totalCount =
                await searchCollection
                    .CountDocumentsAsync(filter);

            // 6) Tri + pagination
            SortDefinition<SearchItem> sort =
                Builders<SearchItem>
                    .Sort.Descending(si => si.UpdatedAt);

            FindOptions<SearchItem> findOptions = new()
            {
                Sort = sort,
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            };

            IAsyncCursor<SearchItem> cursor =
                await searchCollection.FindAsync(filter, findOptions);

            List<SearchItem> items =
                await cursor.ToListAsync();

            return (items, totalCount);
        }

    }
}
