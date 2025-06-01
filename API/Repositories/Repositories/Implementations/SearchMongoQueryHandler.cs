using Entities.Model.Searching;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;
using System.Text.RegularExpressions;

namespace Repositories.Implementations
{
    public class SearchMongoQueryHandler : ISearchQueryHandler
    {
        private readonly IMongoCollection<SearchItem> _searchCollection;

        public SearchMongoQueryHandler(IMongoDatabase database, IMongoDbSettings settings)
        {
            _searchCollection = database.GetCollection<SearchItem>(settings.SearchItemCollectionName);
        }

        public async Task<(IEnumerable<SearchItem> Items, long TotalCount)> SearchAsync(
            string? query,
            string[]? categories,
            int page,
            int pageSize)
        {
            // 1) Filtre initial vide
            FilterDefinition<SearchItem> filter = Builders<SearchItem>.Filter.Empty;

            // 2) Si query non vide, on utilise une regex insensible à la casse sur Title
            if (!string.IsNullOrWhiteSpace(query))
            {
                // Échapper les caractères spéciaux dans la chaîne de recherche
                var escaped = Regex.Escape(query.Trim());
                // Filtre : Title contient la sous-chaîne (case-insensitive)
                BsonRegularExpression regex = new BsonRegularExpression($".*{escaped}.*", "i");
                filter = Builders<SearchItem>.Filter.Regex(si => si.Title, regex);
            }

            // 3) Si catégories fournies, on filtre dessus
            if (categories != null && categories.Length > 0)
            {
                FilterDefinition<SearchItem>? catFilter = Builders<SearchItem>.Filter.In(x => x.Category, categories);
                filter = filter & catFilter;
            }

            // 4) Calculer le total avant pagination
            var totalCount = await _searchCollection.CountDocumentsAsync(filter);

            // 5) Définir le tri : par UpdatedAt décroissant (on ne peut pas trier par score textuel ici)
            SortDefinition<SearchItem>? sort = Builders<SearchItem>.Sort.Descending(x => x.UpdatedAt);

            // 6) Appliquer pagination
            FindOptions<SearchItem> findOptions = new FindOptions<SearchItem>
            {
                Sort = sort,
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            };

            IAsyncCursor<SearchItem>? cursor = await _searchCollection.FindAsync(filter, findOptions);
            List<SearchItem>? items = await cursor.ToListAsync();
            return (items, totalCount);
        }
    }
}