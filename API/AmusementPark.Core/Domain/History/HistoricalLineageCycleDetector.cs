namespace AmusementPark.Core.Domain.History;

public static class HistoricalLineageCycleDetector
{
    public static bool HasDirectedCycle(IReadOnlyCollection<HistoricalRelation> relations)
    {
        ArgumentNullException.ThrowIfNull(relations);
        Dictionary<HistoricalSubjectKey, HistoricalSubjectKey[]> adjacency = relations
            .Where(static relation => relation.Direction == HistoricalRelationDirection.Directed)
            .GroupBy(static relation => new HistoricalSubjectKey(relation.Source.Type, relation.Source.Id))
            .ToDictionary(
                static group => group.Key,
                static group => group.Select(relation => new HistoricalSubjectKey(
                        relation.Target.Type,
                        relation.Target.Id))
                    .Distinct()
                    .ToArray());
        HashSet<HistoricalSubjectKey> visited = new();
        HashSet<HistoricalSubjectKey> active = new();
        return adjacency.Keys.Any(node => Visit(node, adjacency, visited, active));
    }

    private static bool Visit(
        HistoricalSubjectKey node,
        IReadOnlyDictionary<HistoricalSubjectKey, HistoricalSubjectKey[]> adjacency,
        ISet<HistoricalSubjectKey> visited,
        ISet<HistoricalSubjectKey> active)
    {
        if (active.Contains(node))
        {
            return true;
        }

        if (!visited.Add(node))
        {
            return false;
        }

        active.Add(node);
        bool hasCycle = adjacency.GetValueOrDefault(node, Array.Empty<HistoricalSubjectKey>())
            .Any(target => Visit(target, adjacency, visited, active));
        active.Remove(node);
        return hasCycle;
    }
}
