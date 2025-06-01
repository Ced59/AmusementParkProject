using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Entities.Model.Searching;
using MongoDB.Bson;
using MongoDB.Driver;
using Repositories.Interfaces;

namespace Repositories.Implementations
{
    public class SearchMongoQueryHandler : ISearchQueryHandler
    {
        private readonly IMongoCollection<SearchItem> _searchCollection;

        public SearchMongoQueryHandler(
            IMongoDatabase database,
            IMongoDbSettings settings)
        {
            _searchCollection = database
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

            // 2) Si l’utilisateur a saisi quelque chose, on ajoute le filtre
            //    “Title contient la sous-chaîne (case insensible)”
            if (!string.IsNullOrWhiteSpace(query))
            {
                string escaped = Regex.Escape(query.Trim());
                BsonRegularExpression regex = new($".*{escaped}.*", "i");

                filter = Builders<SearchItem>
                    .Filter.Regex(si => si.Title, regex);
            }

            // 3) Si des catégories sont fournies, on ajoute un seul Filter.In.
            //    Cela correspond à une requête “Category == cat1 OR Category == cat2 OR …”
            if (categories != null && categories.Length > 0)
            {
                FilterDefinition<SearchItem> catFilter =
                    Builders<SearchItem>
                        .Filter.In(si => si.Category, categories);

                // On combine le filtre texte (s’il y en a un) ET le catFilter
                filter = filter & catFilter;
            }

            // 4) Calcul du nombre total de documents correspondant (avant pagination)
            long totalCount =
                await _searchCollection
                    .CountDocumentsAsync(filter);

            // 5) Définition du tri : par UpdatedAt décroissant
            SortDefinition<SearchItem> sort =
                Builders<SearchItem>
                    .Sort.Descending(si => si.UpdatedAt);

            // 6) Application de la pagination
            FindOptions<SearchItem> findOptions = new()
            {
                Sort = sort,
                Skip = (page - 1) * pageSize,
                Limit = pageSize
            };

            IAsyncCursor<SearchItem> cursor =
                await _searchCollection.FindAsync(filter, findOptions);

            List<SearchItem> items =
                await cursor.ToListAsync();

            return (items, totalCount);
        }
    }
}
