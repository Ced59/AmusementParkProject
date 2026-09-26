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
        HashSet<string> initialValues = BuildInitialValues(possibleFirstFacts, reasons);
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

    private static HashSet<string> BuildInitialValues(
        IReadOnlyCollection<HistoricalFact> possibleFirstFacts,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<string> initialValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (HistoricalFact fact in possibleFirstFacts)
        {
            bool parsed = HistoricalAttributeTransitionParser.TryParse(
                    fact,
                    out string? previousValue,
                    out _);
            if (parsed)
            {
                initialValues.Add(previousValue ?? UnknownValue);
                if (fact.State != HistoricalFactState.Verified)
                {
                    initialValues.Add(UnknownValue);
                }
            }
            else
            {
                reasons.Add(HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue, fact);
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

            if (applicability == HistoricalTransitionApplicability.NotOccurred
                && EstablishesPreviousValueOnRequestedDate(fact, requestedDate)
                && !ShouldSuppressSequencedPreviousValue(fact, group))
            {
                contributingFactIds.Add(fact.Id);
                AddPreviousValue(result, fact, reasons);
            }

            result = this.ApplyAccordingToApplicability(
                result,
                fact,
                applicability,
                requestedDate,
                reasons);
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
        List<HistoricalFact> currentValueTransitions = new();
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
            else if (EstablishesPreviousValueOnRequestedDate(fact, requestedDate))
            {
                currentValueTransitions.Add(fact);
            }
        }

        currentValueTransitions.RemoveAll(currentFact => applicableTransitions.Any(
            transition => transition.Applicability == HistoricalTransitionApplicability.Applied
                && HistoricalTransitionOrdering.MustPrecede(currentFact, transition.Fact)));
        currentValueTransitions.RemoveAll(currentFact =>
            ShouldSuppressSequencedPreviousValue(currentFact, group));
        foreach (HistoricalFact fact in currentValueTransitions)
        {
            contributingFactIds.Add(fact.Id);
        }

        if (applicableTransitions.Count == 0 && currentValueTransitions.Count == 0)
        {
            return values;
        }

        HistoricalFact[] decisionFacts = applicableTransitions
            .Select(static transition => transition.Fact)
            .Concat(currentValueTransitions)
            .ToArray();
        if (decisionFacts.Length > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.AmbiguousAttributeOrder,
                decisionFacts);
        }

        HashSet<string> preservedValues = new HashSet<string>(StringComparer.Ordinal);
        foreach (HistoricalFact fact in currentValueTransitions)
        {
            AddPreviousValue(preservedValues, fact, reasons);
        }

        if (applicableTransitions.Count == 0)
        {
            preservedValues.UnionWith(values);
            return preservedValues;
        }

        if (applicableTransitions.Count > MaximumExactPermutationGroupSize)
        {
            HashSet<string> largeGroupResult = this.ApplyLargeUnorderedGroup(
                values,
                applicableTransitions,
                requestedDate,
                reasons);
            largeGroupResult.UnionWith(preservedValues);
            return largeGroupResult;
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
                    HashSet<string> nextValues = this.ApplyTransition(
                        currentValues,
                        fact,
                        requestedDate,
                        reasons);
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

        result.UnionWith(preservedValues);
        return result;
    }

    private static void AddPreviousValue(
        ISet<string> values,
        HistoricalFact fact,
        HistoricalSnapshotReasonCollector reasons)
    {
        bool parsed = HistoricalAttributeTransitionParser.TryParse(
            fact,
            out string? previousValue,
            out _);
        if (parsed)
        {
            values.Add(previousValue ?? UnknownValue);
            return;
        }

        reasons.Add(HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue, fact);
        values.Add(UnknownValue);
    }

    private static bool EstablishesPreviousValueOnRequestedDate(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        if (fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.LastDayOfPreviousValue)
        {
            return requestedDate >= (envelope.EarliestPossibleDate ?? DateOnly.MinValue)
                && requestedDate <= (envelope.LatestPossibleDate ?? DateOnly.MaxValue);
        }

        DateOnly? firstDayOfNewValue = envelope.IsExactDay
            ? envelope.EarliestPossibleDate
            : null;
        return fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.FirstDayOfNewValue
            && firstDayOfNewValue.HasValue
            && firstDayOfNewValue.Value != DateOnly.MinValue
            && firstDayOfNewValue.Value.AddDays(-1) == requestedDate;
    }

    private static bool HasSequencedSameDayPredecessor(
        HistoricalFact fact,
        IReadOnlyCollection<HistoricalFact> group)
    {
        if (!fact.SequenceWithinDate.HasValue)
        {
            return false;
        }

        HistoricalDateEnvelope factEnvelope = fact.Period.GetPossibleEnvelope();
        return factEnvelope.IsExactDay
            && group.Any(candidate => candidate.Id != fact.Id
                && candidate.SequenceWithinDate.HasValue
                && candidate.SequenceWithinDate.Value < fact.SequenceWithinDate.Value
                && candidate.Period.GetPossibleEnvelope() == factEnvelope);
    }

    private static bool ShouldSuppressSequencedPreviousValue(
        HistoricalFact fact,
        IReadOnlyCollection<HistoricalFact> group)
    {
        return fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.FirstDayOfNewValue
            && HasSequencedSameDayPredecessor(fact, group);
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
        DateOnly requestedDate,
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

            result.UnionWith(this.ApplyTransition(values, fact, requestedDate, reasons));
        }

        return result;
    }

    private HashSet<string> ApplyAccordingToApplicability(
        HashSet<string> values,
        HistoricalFact fact,
        HistoricalTransitionApplicability applicability,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (applicability == HistoricalTransitionApplicability.NotOccurred)
        {
            return values;
        }

        HashSet<string> applied = this.ApplyTransition(values, fact, requestedDate, reasons);
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
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (!HistoricalAttributeTransitionParser.TryParse(
                fact,
                out string? previousValue,
                out string? nextValue)
            || nextValue is null)
        {
            reasons.Add(HistoricalSnapshotReasonCode.InvalidStructuredAttributeValue, fact);
            return new HashSet<string>(StringComparer.Ordinal) { UnknownValue };
        }

        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        bool isInsideLastDayEnvelope = fact.AttributeBoundaryMeaning
                == AttributeBoundaryMeaning.LastDayOfPreviousValue
            && requestedDate >= (envelope.EarliestPossibleDate ?? DateOnly.MinValue)
            && requestedDate <= (envelope.LatestPossibleDate ?? DateOnly.MaxValue);
        if (isInsideLastDayEnvelope && previousValue is not null)
        {
            HashSet<string> boundaryValues = new HashSet<string>(StringComparer.Ordinal)
            {
                previousValue,
            };
            if (!envelope.IsExactDay)
            {
                boundaryValues.Add(nextValue);
            }

            return boundaryValues;
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
