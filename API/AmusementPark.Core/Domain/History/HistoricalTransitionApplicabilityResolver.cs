namespace AmusementPark.Core.Domain.History;

internal static class HistoricalTransitionApplicabilityResolver
{
    internal static HistoricalTransitionApplicability ResolveLifecycle(
        HistoricalFact fact,
        DateOnly requestedDate,
        bool temporaryClosureHasKnownEnd)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly earliest = envelope.EarliestPossibleDate ?? DateOnly.MinValue;
        DateOnly latest = envelope.LatestPossibleDate ?? DateOnly.MaxValue;
        if (requestedDate < earliest)
        {
            return HasUncertainNonDirectionalPointBoundary(fact)
                ? HistoricalTransitionApplicability.Optional
                : HistoricalTransitionApplicability.NotOccurred;
        }

        if (fact.State != HistoricalFactState.Verified)
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (requestedDate > latest)
        {
            return HasConfirmedBoundary(fact)
                ? HistoricalTransitionApplicability.Applied
                : HistoricalTransitionApplicability.Optional;
        }

        if (HasConfirmedBoundary(fact))
        {
            if (requestedDate == earliest
                && fact.LifecycleBoundaryMeaning == LifecycleBoundaryMeaning.LastOperatingDay)
            {
                return HistoricalTransitionApplicability.Applied;
            }

            if (requestedDate == latest
                && fact.LifecycleBoundaryMeaning is (LifecycleBoundaryMeaning.FirstOperatingDay
                    or LifecycleBoundaryMeaning.FirstClosedDay)
                && (fact.Type != HistoricalFactType.TemporaryClosure
                    || temporaryClosureHasKnownEnd))
            {
                return HistoricalTransitionApplicability.Applied;
            }
        }

        if (!IsEvidenceCertain(fact))
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (envelope.IsExactDay)
        {
            return ResolveExactLifecycleBoundary(fact);
        }

        if (fact.Type == HistoricalFactType.TemporaryClosure && !temporaryClosureHasKnownEnd)
        {
            return HistoricalTransitionApplicability.Optional;
        }

        return HistoricalTransitionApplicability.Optional;
    }

    internal static HistoricalTransitionApplicability ResolveAttribute(
        HistoricalFact fact,
        DateOnly requestedDate)
    {
        HistoricalDateEnvelope envelope = fact.Period.GetPossibleEnvelope();
        DateOnly earliest = envelope.EarliestPossibleDate ?? DateOnly.MinValue;
        DateOnly latest = envelope.LatestPossibleDate ?? DateOnly.MaxValue;
        if (requestedDate < earliest)
        {
            return HasUncertainNonDirectionalPointBoundary(fact)
                ? HistoricalTransitionApplicability.Optional
                : HistoricalTransitionApplicability.NotOccurred;
        }

        if (fact.State != HistoricalFactState.Verified)
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (requestedDate > latest)
        {
            return HasConfirmedBoundary(fact)
                ? HistoricalTransitionApplicability.Applied
                : HistoricalTransitionApplicability.Optional;
        }

        if (HasConfirmedBoundary(fact))
        {
            if (requestedDate == earliest
                && fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.LastDayOfPreviousValue)
            {
                return HistoricalTransitionApplicability.NotOccurred;
            }

            if (requestedDate == latest
                && fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.FirstDayOfNewValue)
            {
                return HistoricalTransitionApplicability.Applied;
            }
        }

        if (!IsEvidenceCertain(fact))
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (envelope.IsExactDay)
        {
            return fact.AttributeBoundaryMeaning switch
            {
                AttributeBoundaryMeaning.FirstDayOfNewValue => HistoricalTransitionApplicability.Applied,
                AttributeBoundaryMeaning.LastDayOfPreviousValue => HistoricalTransitionApplicability.NotOccurred,
                _ => HistoricalTransitionApplicability.Optional,
            };
        }

        return HistoricalTransitionApplicability.Optional;
    }

    internal static bool IsEvidenceCertain(HistoricalFact fact)
    {
        return HasConfirmedBoundary(fact)
            && !fact.Period.Start!.Qualifier.HasValue;
    }

    internal static bool HasConfirmedBoundary(HistoricalFact fact)
    {
        return fact.State == HistoricalFactState.Verified
            && fact.Period.IsPoint
            && fact.Period.StartConfidence == PeriodBoundaryConfidence.Confirmed
            && fact.Period.EndConfidence == PeriodBoundaryConfidence.Confirmed
            && fact.Period.Start is { IsApproximate: false };
    }

    private static bool HasUncertainNonDirectionalPointBoundary(HistoricalFact fact)
    {
        return fact.Period.IsPoint
            && fact.Period.Start is { Qualifier: null } boundary
            && (boundary.IsApproximate
                || fact.Period.StartConfidence != PeriodBoundaryConfidence.Confirmed
                || fact.Period.EndConfidence != PeriodBoundaryConfidence.Confirmed);
    }

    private static HistoricalTransitionApplicability ResolveExactLifecycleBoundary(HistoricalFact fact)
    {
        return fact.LifecycleBoundaryMeaning switch
        {
            LifecycleBoundaryMeaning.FirstOperatingDay => HistoricalTransitionApplicability.Applied,
            LifecycleBoundaryMeaning.FirstClosedDay => HistoricalTransitionApplicability.Applied,
            LifecycleBoundaryMeaning.LastOperatingDay => HistoricalTransitionApplicability.Applied,
            _ => HistoricalTransitionApplicability.Optional,
        };
    }
}
