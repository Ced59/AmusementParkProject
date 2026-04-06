using System.Text.RegularExpressions;
using AmusementPark.Application.Common.Results;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Search.Results;
using AmusementPark.Infrastructure.Configuration.Mongo;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Search;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using MongoDB.Driver;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

/// <summary>
/// Repository Mongo de lecture pour la recherche.
/// </summary>
public sealed class SearchReadRepository : ISearchReadRepository
{
    private readonly IMongoCollection<SearchItemDocument> collection;

    public SearchReadRepository(IMongoDatabase database, MongoDbSettings settings)
    {
        this.collection = database.GetCollection<SearchItemDocument>(settings.SearchItemCollectionName);
    }

    public async Task<SearchResultPage<SearchHitResult>> SearchAsync(string text, IReadOnlyCollection<string> categories, int page, int pageSize, CancellationToken cancellationToken)
    {
        FilterDefinition<SearchItemDocument> filter = Builders<SearchItemDocument>.Filter.Eq(document => document.IsVisible, true);

        if (!string.IsNullOrWhiteSpace(text))
        {
            string escapedQuery = Regex.Escape(text.Trim());
            BsonRegularExpression regex = new BsonRegularExpression($".*{escapedQuery}.*", "i");

            FilterDefinition<SearchItemDocument> textFilter = Builders<SearchItemDocument>.Filter.Or(
                Builders<SearchItemDocument>.Filter.Regex(document => document.Title, regex),
                Builders<SearchItemDocument>.Filter.Regex(document => document.Subtitle, regex),
                Builders<SearchItemDocument>.Filter.Regex(document => document.Description, regex),
                Builders<SearchItemDocument>.Filter.Regex("keywords", regex));

            filter &= textFilter;
        }

        if (categories.Count > 0)
        {
            string[] normalizedCategories = categories
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (normalizedCategories.Length > 0)
            {
                filter &= Builders<SearchItemDocument>.Filter.In(document => document.Category, normalizedCategories);
            }
        }

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        List<SearchItemDocument> documents = await this.collection.Find(filter)
            .SortByDescending(document => document.CompositeScore)
            .ThenByDescending(document => document.UpdatedAt)
            .Skip((page - 1) * pageSize)
            .Limit(pageSize)
            .ToListAsync(cancellationToken);

        return new SearchResultPage<SearchHitResult>(
            documents.Select(document => document.ToSearchHit()).ToList(),
            page,
            pageSize,
            totalItems);
    }
}
