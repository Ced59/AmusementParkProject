namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalAttributeSnapshotReducer
{
    private const string UnknownValue = "\u0000";
    private const int MaximumExactPermutationGroupSize = 7;

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
        HistoricalFact[] possibleFirstFacts = FindPossibleFirstFacts(facts);
        HashSet<string> initialValues = BuildInitialValues(possibleFirstFacts);
        HashSet<Guid> contributingFactIds = possibleFirstFacts
            .Select(static fact => fact.Id)
            .ToHashSet();

        HashSet<string> valuesAcrossRequest = new HashSet<string>(StringComparer.Ordinal);
        foreach (DateOnly requestedDate in requestedDates)
        {
            HashSet<string> values = new HashSet<string>(initialValues, StringComparer.Ordinal);
            foreach (IReadOnlyList<HistoricalFact> group in transitionGroups)
            {
                values = HistoricalTransitionOrdering.CanUseExplicitSequence(group)
                    ? this.ApplyOrderedGroup(
                        values,
                        group,
                        requestedDate,
                        reasons,
                        contributingFactIds)
                    : this.ApplyUnorderedGroup(
                        values,
                        group,
                        requestedDate,
                        reasons,
                        contributingFactIds);
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

        return new HistoricalAttributeSnapshot(
            kind,
            state,
            value,
            candidates,
            reasons.Build(),
            contributingFactIds);
    }

    private static HistoricalFact[] FindPossibleFirstFacts(IReadOnlyCollection<HistoricalFact> facts)
    {
        return facts
            .Where(candidate => !facts.Any(other => other.Id != candidate.Id
                && HistoricalTransitionOrdering.MustPrecede(other, candidate)))
            .ToArray();
    }

    private static HashSet<string> BuildInitialValues(IReadOnlyCollection<HistoricalFact> possibleFirstFacts)
    {
        HashSet<string> initialValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (HistoricalFact fact in possibleFirstFacts)
        {
            if (HistoricalTransitionApplicabilityResolver.IsEvidenceCertain(fact)
                && HistoricalAttributeTransitionParser.TryParse(
                    fact,
                    out string? previousValue,
                    out _)
                && previousValue is not null)
            {
                initialValues.Add(previousValue);
            }
            else
            {
                initialValues.Add(UnknownValue);
            }
        }

        if (initialValues.Count == 0)
        {
            initialValues.Add(UnknownValue);
        }

        return initialValues;
    }

    private HashSet<string> ApplyOrderedGroup(
        HashSet<string> values,
        IReadOnlyList<HistoricalFact> group,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons,
        ISet<Guid> contributingFactIds)
    {
        HashSet<string> result = new HashSet<string>(values, StringComparer.Ordinal);
        foreach (HistoricalFact fact in HistoricalTransitionOrdering.OrderByExplicitSequence(group))
        {
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveAttribute(fact, requestedDate);
            RecordApplicabilityReason(fact, applicability, reasons);
            if (applicability != HistoricalTransitionApplicability.NotOccurred)
            {
                contributingFactIds.Add(fact.Id);
            }

            result = this.ApplyAccordingToApplicability(result, fact, applicability, reasons);
        }

        return result;
    }

    private HashSet<string> ApplyUnorderedGroup(
        HashSet<string> values,
        IReadOnlyList<HistoricalFact> group,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons,
        ISet<Guid> contributingFactIds)
    {
        List<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability)> applicableTransitions = new();
        foreach (HistoricalFact fact in group)
        {
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveAttribute(fact, requestedDate);
            RecordApplicabilityReason(fact, applicability, reasons);
            if (applicability != HistoricalTransitionApplicability.NotOccurred)
            {
                contributingFactIds.Add(fact.Id);
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

        HashSet<string> result = new HashSet<string>(StringComparer.Ordinal);
        for (int targetMask = 0; targetMask <= allMask; targetMask++)
        {
            if ((targetMask & requiredMask) != requiredMask)
            {
                continue;
            }

            Dictionary<int, HashSet<string>> valuesByMask = new()
            {
                [0] = new HashSet<string>(values, StringComparer.Ordinal),
            };
            for (int mask = 0; mask <= targetMask; mask++)
            {
                if ((mask & ~targetMask) != 0
                    || !valuesByMask.TryGetValue(mask, out HashSet<string>? currentValues))
                {
                    continue;
                }

                for (int index = 0; index < applicableTransitions.Count; index++)
                {
                    int bit = 1 << index;
                    if ((targetMask & bit) == 0
                        || (mask & bit) != 0
                        || !PredecessorsWereApplied(applicableTransitions, targetMask, mask, index))
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

            if (valuesByMask.TryGetValue(targetMask, out HashSet<string>? finalValues))
            {
                result.UnionWith(finalValues);
            }
        }

        return result;
    }

    private static bool PredecessorsWereApplied(
        IReadOnlyList<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability)> transitions,
        int targetMask,
        int appliedMask,
        int candidateIndex)
    {
        HistoricalFact candidate = transitions[candidateIndex].Fact;
        for (int index = 0; index < transitions.Count; index++)
        {
            int bit = 1 << index;
            if ((targetMask & bit) != 0
                && (appliedMask & bit) == 0
                && HistoricalTransitionOrdering.MustPrecede(transitions[index].Fact, candidate))
            {
                return false;
            }
        }

        return true;
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
        HistoricalFact[] requiredFacts = transitions
            .Where(static transition => transition.Applicability == HistoricalTransitionApplicability.Applied)
            .Select(static transition => transition.Fact)
            .ToArray();
        foreach ((HistoricalFact fact, _) in transitions)
        {
            if (requiredFacts.Any(required => HistoricalTransitionOrdering.MustPrecede(fact, required)))
            {
                continue;
            }

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
