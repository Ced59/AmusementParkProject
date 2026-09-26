namespace AmusementPark.Core.Domain.History;

internal static class HistoricalTransitionOrdering
{
    internal static IReadOnlyList<IReadOnlyList<HistoricalFact>> BuildGroups(
        IReadOnlyCollection<HistoricalFact> facts)
    {
        HistoricalFact[] orderedFacts = facts
            .OrderBy(static fact => Earliest(fact))
            .ThenBy(static fact => Latest(fact))
            .ThenBy(static fact => fact.Type)
            .ThenBy(static fact => fact.Id)
            .ToArray();
        List<IReadOnlyList<HistoricalFact>> groups = new();
        List<HistoricalFact> current = new();
        DateOnly currentLatest = DateOnly.MinValue;
        foreach (HistoricalFact fact in orderedFacts)
        {
            DateOnly earliest = Earliest(fact);
            DateOnly latest = Latest(fact);
            if (current.Count > 0 && earliest > currentLatest)
            {
                groups.Add(current.ToArray());
                current = new List<HistoricalFact>();
                currentLatest = latest;
            }

            current.Add(fact);
            if (latest > currentLatest)
            {
                currentLatest = latest;
            }
        }

        if (current.Count > 0)
        {
            groups.Add(current.ToArray());
        }

        return groups;
    }

    internal static bool CanUseExplicitSequence(IReadOnlyCollection<HistoricalFact> facts)
    {
        if (facts.Count <= 1)
        {
            return true;
        }

        HistoricalFact first = facts.First();
        HistoricalDateEnvelope firstEnvelope = first.Period.GetPossibleEnvelope();
        int[] sequences = facts
            .Where(static fact => fact.SequenceWithinDate.HasValue)
            .Select(static fact => fact.SequenceWithinDate!.Value)
            .ToArray();
        return firstEnvelope.IsExactDay
            && facts.All(fact => fact.Period.GetPossibleEnvelope() == firstEnvelope)
            && sequences.Length == facts.Count
            && sequences.Distinct().Count() == sequences.Length;
    }

    internal static IReadOnlyList<HistoricalFact> OrderByExplicitSequence(
        IReadOnlyCollection<HistoricalFact> facts)
    {
        return facts
            .OrderBy(static fact => fact.SequenceWithinDate ?? 0)
            .ThenBy(static fact => fact.Type)
            .ThenBy(static fact => fact.Id)
            .ToArray();
    }

    internal static bool MustPrecede(HistoricalFact candidate, HistoricalFact other)
    {
        HistoricalDateEnvelope candidateEnvelope = candidate.Period.GetPossibleEnvelope();
        HistoricalDateEnvelope otherEnvelope = other.Period.GetPossibleEnvelope();
        if (candidateEnvelope.IsExactDay
            && candidateEnvelope == otherEnvelope
            && candidate.SequenceWithinDate.HasValue
            && other.SequenceWithinDate.HasValue)
        {
            return candidate.SequenceWithinDate.Value < other.SequenceWithinDate.Value;
        }

        return Latest(candidate) < Earliest(other);
    }

    internal static DateOnly Earliest(HistoricalFact fact)
    {
        return fact.Period.GetPossibleEnvelope().EarliestPossibleDate ?? DateOnly.MinValue;
    }

    internal static DateOnly Latest(HistoricalFact fact)
    {
        return fact.Period.GetPossibleEnvelope().LatestPossibleDate ?? DateOnly.MaxValue;
    }
}
