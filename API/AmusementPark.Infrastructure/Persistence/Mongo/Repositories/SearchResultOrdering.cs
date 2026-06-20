using MongoDB.Bson;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal static class SearchResultOrdering
{
    public const string PriorityFieldName = "__searchPriority";

    private static readonly SearchResultPriorityRule[] PriorityRules =
    {
        new SearchResultPriorityRule(10, new[] { "parks" }, new[] { "park" }),
        new SearchResultPriorityRule(20, new[] { "parkitems" }, new[] { "attraction" }),
        new SearchResultPriorityRule(30, new[] { "parkitems" }, new[] { "restaurant" }),
        new SearchResultPriorityRule(40, new[] { "parkitems" }, new[] { "hotel" }),
        new SearchResultPriorityRule(50, new[] { "parkitems" }, new[] { "show" }),
        new SearchResultPriorityRule(60, new[] { "parkitems" }, new[] { "shop" }),
        new SearchResultPriorityRule(70, new[] { "parkitems" }, new[] { "service" }),
        new SearchResultPriorityRule(80, new[] { "parkitems" }, new[] { "transport" }),
        new SearchResultPriorityRule(85, new[] { "parkitems" }, new[] { "animal" }),
        new SearchResultPriorityRule(90, new[] { "operators" }, new[] { "operators", "operator" }),
        new SearchResultPriorityRule(100, new[] { "manufacturers" }, new[] { "manufacturers", "manufacturer" }),
        new SearchResultPriorityRule(110, new[] { "founders" }, new[] { "founders", "founder" }),
    };

    public static int ResolvePriority(string? resourceType, string? category)
    {
        string normalizedResourceType = Normalize(resourceType);
        string normalizedCategory = Normalize(category);

        foreach (SearchResultPriorityRule rule in PriorityRules)
        {
            bool matchesResourceType = rule.ResourceTypes.Count == 0
                || rule.ResourceTypes.Contains(normalizedResourceType, StringComparer.OrdinalIgnoreCase);
            bool matchesCategory = rule.Categories.Count == 0
                || rule.Categories.Contains(normalizedCategory, StringComparer.OrdinalIgnoreCase);

            if (matchesResourceType && matchesCategory)
            {
                return rule.Priority;
            }
        }

        return 120;
    }

    public static BsonDocument BuildPriorityAddFieldsStage()
    {
        BsonArray branches = new BsonArray();

        foreach (SearchResultPriorityRule rule in PriorityRules)
        {
            branches.Add(new BsonDocument
            {
                { "case", BuildPriorityCondition(rule) },
                { "then", rule.Priority },
            });
        }

        return new BsonDocument("$addFields", new BsonDocument(
            PriorityFieldName,
            new BsonDocument("$switch", new BsonDocument
            {
                { "branches", branches },
                { "default", 120 },
            })));
    }

    private static BsonDocument BuildPriorityCondition(SearchResultPriorityRule rule)
    {
        BsonArray groupedConditions = new BsonArray();

        if (rule.ResourceTypes.Count > 0)
        {
            groupedConditions.Add(BuildAnyFieldMatchCondition("resourceType", rule.ResourceTypes));
        }

        if (rule.Categories.Count > 0)
        {
            groupedConditions.Add(BuildAnyFieldMatchCondition("category", rule.Categories));
        }

        if (groupedConditions.Count == 1 && groupedConditions[0].IsBsonDocument)
        {
            return groupedConditions[0].AsBsonDocument;
        }

        return new BsonDocument("$and", groupedConditions);
    }

    private static BsonDocument BuildAnyFieldMatchCondition(string fieldName, IReadOnlyCollection<string> values)
    {
        BsonArray conditions = new BsonArray();

        foreach (string value in values)
        {
            conditions.Add(new BsonDocument("$eq", new BsonArray { BuildLowerFieldExpression(fieldName), value }));
        }

        if (conditions.Count == 1 && conditions[0].IsBsonDocument)
        {
            return conditions[0].AsBsonDocument;
        }

        return new BsonDocument("$or", conditions);
    }

    private static BsonDocument BuildLowerFieldExpression(string fieldName)
    {
        return new BsonDocument("$toLower", new BsonDocument("$ifNull", new BsonArray { $"${fieldName}", string.Empty }));
    }

    private static string Normalize(string? value)
    {
        return (value ?? string.Empty)
            .Trim()
            .Replace(" ", string.Empty, StringComparison.Ordinal)
            .ToLowerInvariant();
    }

    private sealed class SearchResultPriorityRule
    {
        public SearchResultPriorityRule(int priority, IReadOnlyCollection<string> resourceTypes, IReadOnlyCollection<string> categories)
        {
            this.Priority = priority;
            this.ResourceTypes = resourceTypes;
            this.Categories = categories;
        }

        public int Priority { get; }

        public IReadOnlyCollection<string> ResourceTypes { get; }

        public IReadOnlyCollection<string> Categories { get; }
    }
}
