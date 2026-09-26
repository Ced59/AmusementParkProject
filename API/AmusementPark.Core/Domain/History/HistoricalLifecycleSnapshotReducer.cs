namespace AmusementPark.Core.Domain.History;

internal sealed class HistoricalLifecycleSnapshotReducer
{
    private const int MaximumExactPermutationGroupSize = 7;

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
                && fact.State == HistoricalFactState.Verified)
            .OrderBy(static fact => HistoricalTransitionOrdering.Earliest(fact))
            .ThenBy(static fact => fact.Id)
            .FirstOrDefault();
        List<(DateOnly Date, HashSet<HistoricalOperationalState> States)> dailyStates = new();
        HashSet<Guid> contributingFactIds = new HashSet<Guid>();
        foreach (DateOnly requestedDate in requestedDates)
        {
            HashSet<HistoricalOperationalState> states = initialOpening is null
                ? new HashSet<HistoricalOperationalState> { HistoricalOperationalState.Unknown }
                : new HashSet<HistoricalOperationalState> { HistoricalOperationalState.KnownClosed };
            if (initialOpening is not null
                && requestedDate < HistoricalTransitionOrdering.Earliest(initialOpening))
            {
                reasons.Add(HistoricalSnapshotReasonCode.BeforeConfirmedInitialOpening, initialOpening);
                contributingFactIds.Add(initialOpening.Id);
            }

            foreach (IReadOnlyList<HistoricalFact> group in transitionGroups)
            {
                states = HistoricalTransitionOrdering.CanUseExplicitSequence(group)
                    ? this.ApplyOrderedGroup(
                        states,
                        group,
                        lifecycleFacts,
                        requestedDate,
                        reasons,
                        contributingFactIds)
                    : this.ApplyUnorderedGroup(
                        states,
                        group,
                        lifecycleFacts,
                        requestedDate,
                        reasons,
                        contributingFactIds);
            }

            dailyStates.Add((requestedDate, states));
        }

        HistoricalFact[] contributingLifecycleFacts = lifecycleFacts
            .Where(fact => contributingFactIds.Contains(fact.Id))
            .ToArray();
        HistoricalFact[] contributingOpenings = contributingLifecycleFacts
            .Where(static fact => fact.Type == HistoricalFactType.Opening)
            .ToArray();
        if (contributingOpenings.Length > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.InconsistentLifecycleSequence,
                contributingOpenings);
        }

        foreach (HistoricalFact reopening in contributingLifecycleFacts.Where(
                     static fact => fact.Type == HistoricalFactType.Reopening))
        {
            HistoricalFact[] precedingDefinitiveClosures = contributingLifecycleFacts
                .Where(fact => fact.Type == HistoricalFactType.DefinitiveClosure
                    && HistoricalTransitionOrdering.MustPrecede(fact, reopening))
                .ToArray();
            if (precedingDefinitiveClosures.Length > 0)
            {
                reasons.Add(
                    HistoricalSnapshotReasonCode.InconsistentLifecycleSequence,
                    precedingDefinitiveClosures.Append(reopening));
            }
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
            reasons.Add(HistoricalSnapshotReasonCode.ConfirmedActivity, contributingFactIds);
        }
        else if (dailyStates.All(static day => day.States.SetEquals(
                     new[] { HistoricalOperationalState.KnownClosed })))
        {
            resultState = HistoricalOperationalState.KnownClosed;
            reasons.Add(HistoricalSnapshotReasonCode.ConfirmedClosure, contributingFactIds);
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
            contributingFactIds.OrderBy(static id => id).ToArray());
    }

    private HashSet<HistoricalOperationalState> ApplyOrderedGroup(
        HashSet<HistoricalOperationalState> states,
        IReadOnlyList<HistoricalFact> group,
        IReadOnlyCollection<HistoricalFact> lifecycleFacts,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons,
        ISet<Guid> contributingFactIds)
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
            if (applicability == HistoricalTransitionApplicability.NotOccurred
                && IsDayBeforeExactFirstClosedDay(fact, requestedDate))
            {
                contributingFactIds.Add(fact.Id);
                result = ApplyPositiveActivityEvidence(result, fact, reasons);
                continue;
            }

            if (applicability == HistoricalTransitionApplicability.NotOccurred
                && IsDayBeforeExactReopening(fact, requestedDate))
            {
                contributingFactIds.Add(fact.Id);
                result = ApplyNegativeClosureEvidence(result, fact, reasons);
                continue;
            }

            if (applicability != HistoricalTransitionApplicability.NotOccurred)
            {
                contributingFactIds.Add(fact.Id);
            }

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
        HistoricalSnapshotReasonCollector reasons,
        ISet<Guid> contributingFactIds)
    {
        List<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability, bool HasKnownEnd)>
            applicableTransitions = new();
        List<HistoricalFact> positiveActivityFacts = new();
        List<HistoricalFact> negativeClosureFacts = new();
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
                contributingFactIds.Add(fact.Id);
                applicableTransitions.Add((fact, applicability, temporaryClosureHasKnownEnd));
            }
            else if (IsDayBeforeExactFirstClosedDay(fact, requestedDate))
            {
                contributingFactIds.Add(fact.Id);
                positiveActivityFacts.Add(fact);
            }
            else if (IsDayBeforeExactReopening(fact, requestedDate))
            {
                contributingFactIds.Add(fact.Id);
                negativeClosureFacts.Add(fact);
            }
        }

        if (applicableTransitions.Count == 0
            && positiveActivityFacts.Count == 0
            && negativeClosureFacts.Count == 0)
        {
            return states;
        }

        HistoricalFact[] decisionFacts = applicableTransitions
            .Select(static transition => transition.Fact)
            .Concat(positiveActivityFacts)
            .Concat(negativeClosureFacts)
            .ToArray();
        if (decisionFacts.Length > 1)
        {
            reasons.Add(
                HistoricalSnapshotReasonCode.AmbiguousTransitionOrder,
                decisionFacts);
        }

        if (applicableTransitions.Count == 0)
        {
            return ApplyBoundaryEvidence(
                states,
                positiveActivityFacts,
                negativeClosureFacts,
                reasons);
        }

        if (applicableTransitions.Count > MaximumExactPermutationGroupSize)
        {
            HashSet<HistoricalOperationalState> largeGroupResult = this.ApplyLargeUnorderedGroup(
                states,
                applicableTransitions,
                requestedDate,
                reasons);
            return ApplyBoundaryEvidence(
                largeGroupResult,
                positiveActivityFacts,
                negativeClosureFacts,
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

        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>();
        for (int targetMask = 0; targetMask <= allMask; targetMask++)
        {
            if ((targetMask & requiredMask) != requiredMask)
            {
                continue;
            }

            Dictionary<int, HashSet<HistoricalOperationalState>> statesByMask = new()
            {
                [0] = new HashSet<HistoricalOperationalState>(states),
            };
            for (int mask = 0; mask <= targetMask; mask++)
            {
                if ((mask & ~targetMask) != 0
                    || !statesByMask.TryGetValue(mask, out HashSet<HistoricalOperationalState>? currentStates))
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

            if (statesByMask.TryGetValue(targetMask, out HashSet<HistoricalOperationalState>? finalStates))
            {
                result.UnionWith(finalStates);
            }
        }

        return ApplyBoundaryEvidence(
            result,
            positiveActivityFacts,
            negativeClosureFacts,
            reasons);
    }

    private static HashSet<HistoricalOperationalState> ApplyBoundaryEvidence(
        IReadOnlyCollection<HistoricalOperationalState> states,
        IReadOnlyCollection<HistoricalFact> positiveActivityFacts,
        IReadOnlyCollection<HistoricalFact> negativeClosureFacts,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = states.ToHashSet();
        foreach (HistoricalFact fact in positiveActivityFacts)
        {
            result = ApplyPositiveActivityEvidence(result, fact, reasons);
        }

        foreach (HistoricalFact fact in negativeClosureFacts)
        {
            result = ApplyNegativeClosureEvidence(result, fact, reasons);
        }

        return result;
    }

    private static HashSet<HistoricalOperationalState> ApplyPositiveActivityEvidence(
        IReadOnlyCollection<HistoricalOperationalState> states,
        HistoricalFact fact,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = states
            .Where(static state => state != HistoricalOperationalState.Unknown)
            .ToHashSet();
        if (result.Contains(HistoricalOperationalState.KnownClosed))
        {
            reasons.Add(HistoricalSnapshotReasonCode.InconsistentLifecycleSequence, fact);
        }

        result.Add(HistoricalOperationalState.KnownOpen);
        return result;
    }

    private static HashSet<HistoricalOperationalState> ApplyNegativeClosureEvidence(
        IReadOnlyCollection<HistoricalOperationalState> states,
        HistoricalFact fact,
        HistoricalSnapshotReasonCollector reasons)
    {
        HashSet<HistoricalOperationalState> result = states
            .Where(static state => state != HistoricalOperationalState.Unknown)
            .ToHashSet();
        if (result.Contains(HistoricalOperationalState.KnownOpen))
        {
            reasons.Add(HistoricalSnapshotReasonCode.InconsistentLifecycleSequence, fact);
        }

        result.Add(HistoricalOperationalState.KnownClosed);
        return result;
    }

    private static bool PredecessorsWereApplied(
        IReadOnlyList<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability, bool HasKnownEnd)>
            transitions,
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

    private HashSet<HistoricalOperationalState> ApplyLargeUnorderedGroup(
        HashSet<HistoricalOperationalState> states,
        IReadOnlyCollection<(HistoricalFact Fact, HistoricalTransitionApplicability Applicability, bool HasKnownEnd)>
            transitions,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        bool hasRequiredTransition = transitions.Any(
            static transition => transition.Applicability == HistoricalTransitionApplicability.Applied);
        HashSet<HistoricalOperationalState> result = hasRequiredTransition
            ? new HashSet<HistoricalOperationalState>()
            : new HashSet<HistoricalOperationalState>(states);
        HistoricalFact[] requiredFacts = transitions
            .Where(static transition => transition.Applicability == HistoricalTransitionApplicability.Applied)
            .Select(static transition => transition.Fact)
            .ToArray();
        foreach ((HistoricalFact fact, _, bool hasKnownEnd) in transitions)
        {
            if (requiredFacts.Any(required => HistoricalTransitionOrdering.MustPrecede(fact, required)))
            {
                continue;
            }

            result.UnionWith(this.ApplyTransitionToStates(
                states,
                fact,
                hasKnownEnd,
                requestedDate,
                reasons));
        }

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
        HashSet<HistoricalOperationalState> currentStates = states.ToHashSet();
        HashSet<HistoricalOperationalState> result = new HashSet<HistoricalOperationalState>();
        if (IsInsideCoarseLastOperatingEnvelope(fact, requestedDate))
        {
            if (currentStates.Count == 1
                && currentStates.Contains(HistoricalOperationalState.KnownClosed))
            {
                return currentStates;
            }

            result.Add(HistoricalOperationalState.KnownOpen);
            HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
            if (envelope.EarliestPossibleDate == requestedDate)
            {
                if (currentStates.Contains(HistoricalOperationalState.KnownClosed))
                {
                    result.Add(HistoricalOperationalState.KnownClosed);
                }

                return result;
            }
        }

        foreach (HistoricalOperationalState state in currentStates)
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

    private static bool IsInsideCoarseLastOperatingEnvelope(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly earliest = envelope.EarliestPossibleDate ?? DateOnly.MinValue;
        DateOnly latest = envelope.LatestPossibleDate ?? DateOnly.MaxValue;
        return fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.LastOperatingDay
            && !envelope.IsExactDay
            && requestedDate >= earliest
            && requestedDate <= latest;
    }

    private HistoricalOperationalState ApplyTransition(
        HistoricalOperationalState currentState,
        HistoricalFact fact,
        bool temporaryClosureHasKnownEnd,
        DateOnly requestedDate,
        HistoricalSnapshotReasonCollector reasons)
    {
        if (IsExactLastOperatingDay(fact, requestedDate))
        {
            return HistoricalOperationalState.KnownOpen;
        }

        if (IsDayAfterExactLastOperatingDay(fact, requestedDate))
        {
            RecordClosureAgainstClosedState(currentState, fact, reasons);
            return HistoricalOperationalState.KnownClosed;
        }

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

        if (fact.Type == HistoricalFactType.Closure
            && (temporaryClosureHasKnownEnd || IsExactFirstClosedDay(fact, requestedDate)))
        {
            RecordClosureAgainstClosedState(currentState, fact, reasons);
            return HistoricalOperationalState.KnownClosed;
        }

        RecordClosureAgainstClosedState(currentState, fact, reasons);
        reasons.Add(HistoricalSnapshotReasonCode.UnclassifiedClosure, fact);
        return HistoricalOperationalState.Unknown;
    }

    private static bool IsExactLastOperatingDay(HistoricalFact fact, DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        return fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.LastOperatingDay
            && envelope.IsExactDay
            && envelope.EarliestPossibleDate == requestedDate;
    }

    private static bool IsDayAfterExactLastOperatingDay(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly? lastOperatingDay = envelope.EarliestPossibleDate;
        return fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.LastOperatingDay
            && envelope.IsExactDay
            && lastOperatingDay.HasValue
            && lastOperatingDay.Value != DateOnly.MaxValue
            && lastOperatingDay.Value.AddDays(1) == requestedDate;
    }

    private static bool IsDayBeforeExactFirstClosedDay(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly? firstClosedDay = envelope.EarliestPossibleDate;
        return fact.State == HistoricalFactState.Verified
            && fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.FirstClosedDay
            && envelope.IsExactDay
            && firstClosedDay.HasValue
            && firstClosedDay.Value != DateOnly.MinValue
            && firstClosedDay.Value.AddDays(-1) == requestedDate;
    }

    private static bool IsDayBeforeExactReopening(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly? firstOperatingDay = envelope.EarliestPossibleDate;
        return fact.State == HistoricalFactState.Verified
            && fact.Type == HistoricalFactType.Reopening
            && fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.FirstOperatingDay
            && envelope.IsExactDay
            && firstOperatingDay.HasValue
            && firstOperatingDay.Value != DateOnly.MinValue
            && firstOperatingDay.Value.AddDays(-1) == requestedDate;
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
        if (fact.Type is not HistoricalFactType.TemporaryClosure
            and not HistoricalFactType.Closure)
        {
            return false;
        }

        return lifecycleFacts.Any(candidate => candidate.Type == HistoricalFactType.Reopening
            && HistoricalTransitionApplicabilityResolver.HasConfirmedBoundary(candidate)
            && HistoricalTransitionOrdering.MustPrecede(fact, candidate));
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
