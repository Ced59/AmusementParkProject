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
    private static readonly string[] ParkItemCategories = new[]
    {
        "attraction",
        "restaurant",
        "hotel",
        "animal",
        "show",
        "shop",
        "service",
        "transport",
        "other",
    };

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

        (string[] normalizedCategories, string[] normalizedResourceTypes) = NormalizeRequestedCategories(categories);
        if (normalizedCategories.Length > 0 || normalizedResourceTypes.Length > 0)
        {
            List<FilterDefinition<SearchItemDocument>> categoryFilters = new List<FilterDefinition<SearchItemDocument>>();

            if (normalizedCategories.Length > 0)
            {
                categoryFilters.Add(Builders<SearchItemDocument>.Filter.In(document => document.Category, normalizedCategories));
            }

            if (normalizedResourceTypes.Length > 0)
            {
                categoryFilters.Add(Builders<SearchItemDocument>.Filter.In(document => document.ResourceType, normalizedResourceTypes));
            }

            filter &= Builders<SearchItemDocument>.Filter.Or(categoryFilters);
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

    private static (string[] Categories, string[] ResourceTypes) NormalizeRequestedCategories(IReadOnlyCollection<string> categories)
    {
        HashSet<string> normalizedCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        HashSet<string> normalizedResourceTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (string category in categories)
        {
            if (string.IsNullOrWhiteSpace(category))
            {
                continue;
            }

            string normalized = category.Trim().ToLowerInvariant();
            switch (normalized)
            {
                case "park":
                case "parks":
                    normalizedCategories.Add("park");
                    normalizedResourceTypes.Add("parks");
                    break;

                case "parkitem":
                case "parkitems":
                    AddRange(normalizedCategories, ParkItemCategories);
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "attraction":
                case "attractions":
                    normalizedCategories.Add("attraction");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "restaurant":
                case "restaurants":
                case "resto":
                case "restos":
                    normalizedCategories.Add("restaurant");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "hotel":
                case "hotels":
                    normalizedCategories.Add("hotel");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "animal":
                case "animals":
                    normalizedCategories.Add("animal");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "show":
                case "shows":
                    normalizedCategories.Add("show");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "shop":
                case "shops":
                    normalizedCategories.Add("shop");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "service":
                case "services":
                    normalizedCategories.Add("service");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "transport":
                case "transports":
                    normalizedCategories.Add("transport");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "other":
                case "others":
                    normalizedCategories.Add("other");
                    normalizedResourceTypes.Add("parkItems");
                    break;

                case "operator":
                case "operators":
                    normalizedCategories.Add("operators");
                    normalizedResourceTypes.Add("operators");
                    break;

                case "manufacturer":
                case "manufacturers":
                case "constructor":
                case "constructors":
                case "fabricant":
                case "fabricants":
                    normalizedCategories.Add("manufacturers");
                    normalizedResourceTypes.Add("manufacturers");
                    break;

                case "founder":
                case "founders":
                case "fondateur":
                case "fondateurs":
                    normalizedCategories.Add("founders");
                    normalizedResourceTypes.Add("founders");
                    break;

                default:
                    normalizedCategories.Add(normalized);
                    break;
            }
        }

        return (normalizedCategories.ToArray(), normalizedResourceTypes.ToArray());
    }

    private static void AddRange(HashSet<string> target, IEnumerable<string> values)
    {
        foreach (string value in values)
        {
            target.Add(value);
        }
    }
}
