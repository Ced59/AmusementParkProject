namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalLifecycleSnapshotReducer
{
    private const int MaximumExactPermutationGroupSize = 10;

    internal HistoricalLifecycleReduction Reduce(
        IReadOnlyCollection<HistoricalFact> subjectFacts,
        IReadOnlyList<DateOnly> requestedDates)
    {
        HistoricalFact[] lifecycleFacts = subjectFacts
            .Where(static fact => IsLifecycleTransition(fact.Type))
            .ToArray();
        HistoricalSnapshotReasonCollector reasons = new HistoricalSnapshotReasonCollector();
        if (lifecycleFacts.Length == 0)
        {
            reasons.Add(HistoricalSnapshotReasonCode.NoEligibleLifecycleFact, Array.Empty<Guid>());
            return new HistoricalLifecycleReduction(
                HistoricalOperationalState.Unknown,
                HistoricalPresenceExtent.None,
                Array.Empty<HistoricalPresenceInterval>(),
                reasons.Build(),
                Array.Empty<Guid>());
        }

        IReadOnlyList<IReadOnlyList<HistoricalFact>> transitionGroups =
            HistoricalTransitionOrdering.BuildGroups(lifecycleFacts);
        HistoricalFact? initialOpening = lifecycleFacts
            .Where(static fact => fact.Type == HistoricalFactType.Opening
                && HistoricalTransitionApplicabilityResolver.IsEvidenceCertain(fact))
            .OrderBy(static fact => HistoricalTransitionOrdering.Earliest(fact))
            .ThenBy(static fact => fact.Id)
            .FirstOrDefault();
        if (lifecycleFacts.Count(static fact => fact.Type == HistoricalFactType.Opening) > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.InconsistentLifecycleSequence,
                lifecycleFacts.Where(static fact => fact.Type == HistoricalFactType.Opening));
        }

        foreach (HistoricalFact reopening in lifecycleFacts.Where(
                     static fact => fact.Type == HistoricalFactType.Reopening))
        {
            HistoricalFact[] precedingDefinitiveClosures = lifecycleFacts
                .Where(fact => fact.Type == HistoricalFactType.DefinitiveClosure
                    && HistoricalTransitionOrdering.Latest(fact)
                        < HistoricalTransitionOrdering.Earliest(reopening))
                .ToArray();
            if (precedingDefinitiveClosures.Length > 0)
            {
                reasons.Add(
                    HistoricalSnapshotReasonCode.InconsistentLifecycleSequence,
                    precedingDefinitiveClosures.Append(reopening));
            }
        }

        List<(DateOnly Date, HashSet<HistoricalOperationalState> States)> dailyStates = new();
        foreach (DateOnly requestedDate in requestedDates)
        {
            HashSet<HistoricalOperationalState> states = initialOpening is null
                ? new HashSet<HistoricalOperationalState> { HistoricalOperationalState.Unknown }
                : new HashSet<HistoricalOperationalState> { HistoricalOperationalState.KnownClosed };
            if (initialOpening is not null
                && requestedDate < HistoricalTransitionOrdering.Earliest(initialOpening))
            {
                reasons.Add(HistoricalSnapshotReasonCode.BeforeConfirmedInitialOpening, initialOpening);
            }

            foreach (IReadOnlyList<HistoricalFact> group in transitionGroups)
            {
                states = HistoricalTransitionOrdering.CanUseExplicitSequence(group)
                    ? this.ApplyOrderedGroup(states, group, lifecycleFacts, requestedDate, reasons)
                    : this.ApplyUnorderedGroup(states, group, lifecycleFacts, requestedDate, reasons);
            }

            dailyStates.Add((requestedDate, states));
        }

        DateOnly[] confirmedOpenDates = dailyStates
            .Where(static day => day.States.SetEquals(
                new[] { HistoricalOperationalState.KnownOpen }))
            .Select(static day => day.Date)
            .ToArray();
        HistoricalOperationalState resultState;
        HistoricalPresenceExtent presenceExtent = HistoricalPresenceExtent.None;
        IReadOnlyList<HistoricalPresenceInterval> intervals = Array.Empty<HistoricalPresenceInterval>();
        if (confirmedOpenDates.Length > 0)
        {
            resultState = HistoricalOperationalState.KnownOpen;
            presenceExtent = confirmedOpenDates.Length == requestedDates.Count
                ? HistoricalPresenceExtent.EntireRequestedPeriod
                : HistoricalPresenceExtent.PartOfRequestedPeriod;
            intervals = BuildIntervals(confirmedOpenDates);
            reasons.Add(HistoricalSnapshotReasonCode.ConfirmedActivity, lifecycleFacts);
        }
        else if (dailyStates.All(static day => day.States.SetEquals(
                     new[] { HistoricalOperationalState.KnownClosed })))
        {
            resultState = HistoricalOperationalState.KnownClosed;
            reasons.Add(HistoricalSnapshotReasonCode.ConfirmedClosure, lifecycleFacts);
        }
        else if (dailyStates.Any(static day => day.States.Contains(HistoricalOperationalState.KnownOpen)))
        {
            resultState = HistoricalOperationalState.PossiblyOpen;
        }
        else
        {
            resultState = HistoricalOperationalState.Unknown;
        }

        return new HistoricalLifecycleReduction(
            resultState,
            presenceExtent,
            intervals,
            reasons.Build(),
            lifecycleFacts.Select(static fact => fact.Id).Distinct().OrderBy(static id => id).ToArray());
    }

    private HashSet<HistoricalOperationalState> ApplyOrderedGroup(
        HashSet<HistoricalOperationalState> states,
        IReadOnlyList<HistoricalFact> group,
        IReadOnlyCollection<HistoricalFact> lifecycleFacts,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>(states);
        foreach (HistoricalFact fact in HistoricalTransitionOrdering.OrderByExplicitSequence(group))
        {
            bool temporaryClosureHasKnownEnd = HasKnownReopening(fact, lifecycleFacts);
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveLifecycle(
                    fact,
                    requestedDate,
                    temporaryClosureHasKnownEnd);
            RecordApplicabilityReason(fact, applicability, reasons);
            result = this.ApplyAccordingToApplicability(
                result,
                fact,
                applicability,
                temporaryClosureHasKnownEnd,
                requestedDate,
                reasons);
        }

        return result;
    }

    private HashSet<HistoricalOperationalState> ApplyUnorderedGroup(
        HashSet<HistoricalOperationalState> states,
        IReadOnlyList<HistoricalFact> group,
        IReadOnlyCollection<HistoricalFact> lifecycleFacts,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        List<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability, bool HasKnownEnd)>
            applicableTransitions = new();
        foreach (HistoricalFact fact in group)
        {
            bool temporaryClosureHasKnownEnd = HasKnownReopening(fact, lifecycleFacts);
            HistoricalTransitionApplicability applicability =
                HistoricalTransitionApplicabilityResolver.ResolveLifecycle(
                    fact,
                    requestedDate,
                    temporaryClosureHasKnownEnd);
            RecordApplicabilityReason(fact, applicability, reasons);
            if (applicability != HistoricalTransitionApplicability.NotOccurred)
            {
                applicableTransitions.Add((fact, applicability, temporaryClosureHasKnownEnd));
            }
        }

        if (applicableTransitions.Count == 0)
        {
            return states;
        }

        if (applicableTransitions.Count > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.AmbiguousTransitionOrder,
                applicableTransitions.Select(static transition => transition.Fact));
        }

        if (applicableTransitions.Count > MaximumExactPermutationGroupSize)
        {
            return this.ApplyConservativeClosure(
                states,
                applicableTransitions,
                requestedDate,
                reasons);
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

        Dictionary<int, HashSet<HistoricalOperationalState>> statesByMask = new()
        {
            [0] = new HashSet<HistoricalOperationalState>(states),
        };
        for (int mask = 0; mask <= allMask; mask++)
        {
            if (!statesByMask.TryGetValue(mask, out HashSet<HistoricalOperationalState>? currentStates))
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

                (HistoricalFact fact, _, bool hasKnownEnd) = applicableTransitions[index];
                HashSet<HistoricalOperationalState> nextStates = this.ApplyTransitionToStates(
                    currentStates,
                    fact,
                    hasKnownEnd,
                    requestedDate,
                    reasons);
                int nextMask = mask | bit;
                if (!statesByMask.TryGetValue(nextMask, out HashSet<HistoricalOperationalState>? collected))
                {
                    collected = new HashSet<HistoricalOperationalState>();
                    statesByMask.Add(nextMask, collected);
                }

                collected.UnionWith(nextStates);
            }
        }

        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>();
        foreach (KeyValuePair<int, HashSet<HistoricalOperationalState>> entry in statesByMask)
        {
            if ((entry.Key & requiredMask) == requiredMask)
            {
                result.UnionWith(entry.Value);
            }
        }

        return result;
    }

    private HashSet<HistoricalOperationalState> ApplyConservativeClosure(
        HashSet<HistoricalOperationalState> states,
        IReadOnlyCollection<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability, bool HasKnownEnd)>
            transitions,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>(states);
        bool changed;
        do
        {
            int previousCount = result.Count;
            foreach ((HistoricalFact fact, _, bool hasKnownEnd) in transitions)
            {
                result.UnionWith(this.ApplyTransitionToStates(
                    result,
                    fact,
                    hasKnownEnd,
                    requestedDate,
                    reasons));
            }

            changed = previousCount != result.Count;
        }
        while (changed);

        return result;
    }

    private HashSet<HistoricalOperationalState> ApplyAccordingToApplicability(
        HashSet<HistoricalOperationalState> states,
        HistoricalFact fact,
        HistoricalTransitionApplicability applicability,
        bool temporaryClosureHasKnownEnd,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (applicability == HistoricalTransitionApplicability.NotOccurred)
        {
            return states;
        }

        HashSet<HistoricalOperationalState> applied = this.ApplyTransitionToStates(
            states,
            fact,
            temporaryClosureHasKnownEnd,
            requestedDate,
            reasons);
        if (applicability == HistoricalTransitionApplicability.Applied)
        {
            return applied;
        }

        applied.UnionWith(states);
        return applied;
    }

    private HashSet<HistoricalOperationalState> ApplyTransitionToStates(
        IEnumerable<HistoricalOperationalState> states,
        HistoricalFact fact,
        bool temporaryClosureHasKnownEnd,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>();
        foreach (HistoricalOperationalState state in states)
        {
            result.Add(this.ApplyTransition(
                state,
                fact,
                temporaryClosureHasKnownEnd,
                requestedDate,
                reasons));
        }

        return result;
    }

    private HistoricalOperationalState ApplyTransition(
        HistoricalOperationalState currentState,
        HistoricalFact fact,
        bool temporaryClosureHasKnownEnd,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (fact.Type is HistoricalFactType.Opening or HistoricalFactType.Reopening)
        {
            if (currentState == HistoricalOperationalState.KnownOpen)
            {
                reasons.Add(HistoricalSnapshotReasonCode.InconsistentLifecycleSequence, fact);
            }

            return HistoricalOperationalState.KnownOpen;
        }

        if (fact.Type == HistoricalFactType.DefinitiveClosure)
        {
            RecordClosureAgainstClosedState(currentState, fact, reasons);
            return HistoricalOperationalState.KnownClosed;
        }

        if (fact.Type == HistoricalFactType.TemporaryClosure)
        {
            RecordClosureAgainstClosedState(currentState, fact, reasons);
            if (temporaryClosureHasKnownEnd || IsExactFirstClosedDay(fact, requestedDate))
            {
                return HistoricalOperationalState.KnownClosed;
            }

            reasons.Add(HistoricalSnapshotReasonCode.UnboundedTemporaryClosure, fact);
            return HistoricalOperationalState.Unknown;
        }

        RecordClosureAgainstClosedState(currentState, fact, reasons);
        reasons.Add(HistoricalSnapshotReasonCode.UnclassifiedClosure, fact);
        return HistoricalOperationalState.Unknown;
    }

    private static void RecordClosureAgainstClosedState(
        HistoricalOperationalState currentState,
        HistoricalFact fact,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (currentState == HistoricalOperationalState.KnownClosed)
        {
            reasons.Add(HistoricalSnapshotReasonCode.InconsistentLifecycleSequence, fact);
        }
    }

    private static bool HasKnownReopening(
        HistoricalFact fact,
        IReadOnlyCollection<HistoricalFact> lifecycleFacts)
    {
        if (fact.Type != HistoricalFactType.TemporaryClosure)
        {
            return false;
        }

        DateOnly closureLatest = HistoricalTransitionOrdering.Latest(fact);
        return lifecycleFacts.Any(candidate => candidate.Type == HistoricalFactType.Reopening
            && HistoricalTransitionApplicabilityResolver.IsEvidenceCertain(candidate)
            && HistoricalTransitionOrdering.Earliest(candidate) > closureLatest);
    }

    private static bool IsExactFirstClosedDay(HistoricalFact fact, DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        return envelope.IsExactDay
            && envelope.EarliestPossibleDate == requestedDate
            && fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.FirstClosedDay;
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
                ? HistoricalSnapshotReasonCode.PartialLifecycleBoundary
                : HistoricalSnapshotReasonCode.UncertainEvidence,
            fact);
    }

    private static IReadOnlyList<HistoricalPresenceInterval> BuildIntervals(
        IReadOnlyList<DateOnly> confirmedOpenDates)
    {
        List<HistoricalPresenceInterval> intervals = new();
        DateOnly start = confirmedOpenDates[0];
        DateOnly end = start;
        for (int index = 1; index < confirmedOpenDates.Count; index++)
        {
            DateOnly current = confirmedOpenDates[index];
            if (current == end.AddDays(1))
            {
                end = current;
                continue;
            }

            intervals.Add(new HistoricalPresenceInterval(start, end));
            start = current;
            end = current;
        }

        intervals.Add(new HistoricalPresenceInterval(start, end));
        return intervals;
    }

    private static bool IsLifecycleTransition(HistoricalFactType type)
    {
        return type is HistoricalFactType.Opening
            or HistoricalFactType.Closure
            or HistoricalFactType.Reopening
            or HistoricalFactType.TemporaryClosure
            or HistoricalFactType.DefinitiveClosure;
    }
}
