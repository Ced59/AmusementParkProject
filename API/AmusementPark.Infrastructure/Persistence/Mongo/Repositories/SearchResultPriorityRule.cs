using MongoDB.Bson;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Repositories;

internal sealed class SearchResultPriorityRule
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
