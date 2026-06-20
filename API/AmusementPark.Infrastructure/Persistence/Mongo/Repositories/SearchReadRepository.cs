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

    public async Task<SearchResultPage<SearchHitResult>> SearchAsync(string text, IReadOnlyCollection<string> categories, int page, int pageSize, string languageCode, CancellationToken cancellationToken)
    {
        BsonDocument filter = BuildSearchFilter(text, categories);

        long totalItems = await this.collection.CountDocumentsAsync(filter, cancellationToken: cancellationToken);

        BsonDocument[] pipeline =
        {
            new BsonDocument("$match", filter),
            SearchResultOrdering.BuildPriorityAddFieldsStage(),
            new BsonDocument("$sort", new BsonDocument
            {
                { SearchResultOrdering.PriorityFieldName, 1 },
                { "compositeScore", -1 },
                { "updatedAt", -1 },
            }),
            new BsonDocument("$skip", (page - 1) * pageSize),
            new BsonDocument("$limit", pageSize),
            new BsonDocument("$project", new BsonDocument(SearchResultOrdering.PriorityFieldName, 0)),
        };

        List<SearchItemDocument> documents = await this.collection.Aggregate<SearchItemDocument>(pipeline)
            .ToListAsync(cancellationToken);

        return new SearchResultPage<SearchHitResult>(
            documents.Select(document => document.ToSearchHit(languageCode)).ToList(),
            page,
            pageSize,
            totalItems);
    }

    private static BsonDocument BuildSearchFilter(string text, IReadOnlyCollection<string> categories)
    {
        List<BsonDocument> filters = new List<BsonDocument>
        {
            new BsonDocument("isVisible", true),
        };

        if (!string.IsNullOrWhiteSpace(text))
        {
            string escapedQuery = Regex.Escape(text.Trim());
            BsonRegularExpression regex = new BsonRegularExpression($".*{escapedQuery}.*", "i");

            filters.Add(new BsonDocument("$or", new BsonArray
            {
                new BsonDocument("title", regex),
                new BsonDocument("subtitle", regex),
                new BsonDocument("description", regex),
                new BsonDocument("localizedDescriptions.value", regex),
                new BsonDocument("keywords", regex),
            }));
        }

        (string[] normalizedCategories, string[] normalizedResourceTypes) = NormalizeRequestedCategories(categories);
        if (normalizedCategories.Length > 0 || normalizedResourceTypes.Length > 0)
        {
            BsonArray categoryFilters = new BsonArray();

            if (normalizedCategories.Length > 0)
            {
                categoryFilters.Add(new BsonDocument("category", new BsonDocument("$in", ToBsonArray(normalizedCategories))));
            }

            if (normalizedResourceTypes.Length > 0)
            {
                categoryFilters.Add(new BsonDocument("resourceType", new BsonDocument("$in", ToBsonArray(normalizedResourceTypes))));
            }

            filters.Add(new BsonDocument("$or", categoryFilters));
        }

        if (filters.Count == 1)
        {
            return filters[0];
        }

        return new BsonDocument("$and", new BsonArray(filters));
    }

    private static BsonArray ToBsonArray(IEnumerable<string> values)
    {
        return new BsonArray(values.Select(static value => new BsonString(value)));
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
