namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalAttributeSnapshotReducer
{
    private const string UnknownValue = "\u0000";
    private const int MaximumExactPermutationGroupSize = 10;

    internal IReadOnlyList<HistoricalAttributeSnapshot> Reduce(
        IReadOnlyCollection<HistoricalFact> subjectFacts,
        IReadOnlyList<DateOnly> requestedDates)
    {
        return subjectFacts
            .Where(static fact => fact.AttributeKind.HasValue)
            .GroupBy(static fact => fact.AttributeKind!.Value)
            .OrderBy(static group => group.Key)
            .Select(group => this.ReduceAttribute(group.Key, group.ToArray(), requestedDates))
            .ToArray();
    }

    private HistoricalAttributeSnapshot ReduceAttribute(
        HistoricalAttributeKind kind,
        IReadOnlyCollection<HistoricalFact> facts,
        IReadOnlyList<DateOnly> requestedDates)
    {
        HistoricalSnapshotReasonCollector reasons = new HistoricalSnapshotReasonCollector();
        IReadOnlyList<IReadOnlyList<HistoricalFact>> transitionGroups =
            HistoricalTransitionOrdering.BuildGroups(facts);
        HistoricalFact? firstFact = facts
            .OrderBy(static fact => HistoricalTransitionOrdering.Earliest(fact))
            .ThenBy(static fact => fact.Id)
            .FirstOrDefault();
        string initialValue = UnknownValue;
        if (firstFact is not null
            && HistoricalTransitionApplicabilityResolver.IsEvidenceCertain(firstFact)
            && HistoricalAttributeTransitionParser.TryParse(
                firstFact,
                out string? previousValue,
                out _)
            && previousValue is not null)
        {
            initialValue = previousValue;
        }

        HashSet<string> valuesAcrossRequest = new HashSet<string>(StringComparer.Ordinal);
        foreach (DateOnly requestedDate in requestedDates)
        {
            HashSet<string> values = new HashSet<string>(StringComparer.Ordinal) { initialValue };
            foreach (IReadOnlyList<HistoricalFact> group in transitionGroups)
            {
                values = HistoricalTransitionOrdering.CanUseExplicitSequence(group)
                    ? this.ApplyOrderedGroup(values, group, requestedDate, reasons)
                    : this.ApplyUnorderedGroup(values, group, requestedDate, reasons);
            }

            valuesAcrossRequest.UnionWith(values);
        }

        bool includesUnknown = valuesAcrossRequest.Remove(UnknownValue);
        string[] candidates = valuesAcrossRequest
            .OrderBy(static value => value, StringComparer.Ordinal)
            .ToArray();
        HistoricalAttributeValueState state;
        string? value = null;
        if (candidates.Length == 1 && !includesUnknown)
        {
            state = HistoricalAttributeValueState.Known;
            value = candidates[0];
        }
        else if (candidates.Length > 0)
        {
            state = HistoricalAttributeValueState.Ambiguous;
        }
        else
        {
            state = HistoricalAttributeValueState.Unknown;
        }

        return new HistoricalAttributeSnapshot(kind, state, value, candidates, reasons.Build());
    }

    private HashSet<string> ApplyOrderedGroup(
        HashSet<string> values,
        IReadOnlyList<HistoricalFact> group,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<string> result = new HashSet<string>(values, StringComparer.Ordinal);
        foreach (HistoricalFact fact in HistoricalTransitionOrdering.OrderByExplicitSequence(group))
        {
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveAttribute(fact, requestedDate);
            RecordApplicabilityReason(fact, applicability, reasons);
            result = this.ApplyAccordingToApplicability(result, fact, applicability, reasons);
        }

        return result;
    }

    private HashSet<string> ApplyUnorderedGroup(
        HashSet<string> values,
        IReadOnlyList<HistoricalFact> group,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        List<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability)> applicableTransitions = new();
        foreach (HistoricalFact fact in group)
        {
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveAttribute(fact, requestedDate);
            RecordApplicabilityReason(fact, applicability, reasons);
            if (applicability != HistoricalTransitionApplicability.NotOccurred)
            {
                applicableTransitions.Add((fact, applicability));
            }
        }

        if (applicableTransitions.Count == 0)
        {
            return values;
        }

        if (applicableTransitions.Count > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.AmbiguousAttributeOrder,
                applicableTransitions.Select(static transition => transition.Fact));
        }

        if (applicableTransitions.Count > MaximumExactPermutationGroupSize)
        {
            return this.ApplyLargeUnorderedGroup(values, applicableTransitions, reasons);
        }

        int allMask = (1 << applicableTransitions.Count) - 1;
        int requiredMask = 0;
        for (int index = 0; index < applicableTransitions.Count; index++)
        {
            if (applicableTransitions[index].Applicability == HistoricalTransitionApplicability.Applied)
            {
                requiredMask |= 1 << index;
            }
        }

        Dictionary<int, HashSet<string>> valuesByMask = new()
        {
            [0] = new HashSet<string>(values, StringComparer.Ordinal),
        };
        for (int mask = 0; mask <= allMask; mask++)
        {
            if (!valuesByMask.TryGetValue(mask, out HashSet<string>? currentValues))
            {
                continue;
            }

            for (int index = 0; index < applicableTransitions.Count; index++)
            {
                int bit = 1 << index;
                if ((mask & bit) != 0)
                {
                    continue;
                }

                HistoricalFact fact = applicableTransitions[index].Fact;
                HashSet<string> nextValues = this.ApplyTransition(currentValues, fact, reasons);
                int nextMask = mask | bit;
                if (!valuesByMask.TryGetValue(nextMask, out HashSet<string>? collected))
                {
                    collected = new HashSet<string>(StringComparer.Ordinal);
                    valuesByMask.Add(nextMask, collected);
                }

                collected.UnionWith(nextValues);
            }
        }

        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        foreach (KeyValuePair<int, HashSet<string>> entry in valuesByMask)
        {
            if ((entry.Key & requiredMask) == requiredMask)
            {
                result.UnionWith(entry.Value);
            }
        }

        return result;
    }

    private HashSet<string> ApplyLargeUnorderedGroup(
        HashSet<string> values,
        IReadOnlyCollection<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability)> transitions,
        HistoricalSnapshotReasonCollector reasons)
    {
        bool hasRequiredTransition = transitions.Any(
            static transition => transition.Applicability == HistoricalTransitionApplicability.Applied);
        HashSet<string> result = hasRequiredTransition
            ? new HashSet<string>(StringComparer.Ordinal)
            : new HashSet<string>(values, StringComparer.Ordinal);
        foreach ((HistoricalFact fact, _) in transitions)
        {
            result.UnionWith(this.ApplyTransition(values, fact, reasons));
        }

        return result;
    }

    private HashSet<string> ApplyAccordingToApplicability(
        HashSet<string> values,
        HistoricalFact fact,
        HistoricalTransitionApplicability applicability,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (applicability == HistoricalTransitionApplicability.NotOccurred)
        {
            return values;
        }

        HashSet<string> applied = this.ApplyTransition(values, fact, reasons);
        if (applicability == HistoricalTransitionApplicability.Applied)
        {
            return applied;
        }

        applied.UnionWith(values);
        return applied;
    }

    private HashSet<string> ApplyTransition(
        IEnumerable<string> values,
        HistoricalFact fact,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (!HistoricalAttributeTransitionParser.TryParse(fact, out _, out string? nextValue)
            || nextValue is null)
        {
            reasons.Add(HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue, fact);
            return new HashSet<string>(StringComparer.Ordinal) { UnknownValue };
        }

        return new HashSet<string>(StringComparer.Ordinal) { nextValue };
    }

    private static void RecordApplicabilityReason(
        HistoricalFact fact,
        HistoricalTransitionApplicability applicability,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (applicability != HistoricalTransitionApplicability.Optional)
        {
            return;
        }

        reasons.Add(
            HistoricalTransitionApplicabilityResolver.IsEvidenceCertain(fact)
                ? HistoricalSnapshotReasonCode.PartialAttributeBoundary
                : HistoricalSnapshotReasonCode.UncertainEvidence,
            fact);
    }
}
