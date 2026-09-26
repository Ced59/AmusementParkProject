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
            return HistoricalTransitionApplicability.NotOccurred;
        }

        if (fact.State != HistoricalFactState.Verified)
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (requestedDate > latest)
        {
            return HistoricalTransitionApplicability.Applied;
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

        if (requestedDate == latest
            && fact.LifecycleBoundaryMeaning is LifecycleBoundaryMeaning.FirstOperatingDay
                or LifecycleBoundaryMeaning.FirstClosedDay)
        {
            return HistoricalTransitionApplicability.Applied;
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
            return HistoricalTransitionApplicability.NotOccurred;
        }

        if (fact.State != HistoricalFactState.Verified)
        {
            return HistoricalTransitionApplicability.Optional;
        }

        if (requestedDate > latest)
        {
            return HistoricalTransitionApplicability.Applied;
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

        return requestedDate == latest
            && fact.AttributeBoundaryMeaning == AttributeBoundaryMeaning.FirstDayOfNewValue
                ? HistoricalTransitionApplicability.Applied
                : HistoricalTransitionApplicability.Optional;
    }

    internal static bool IsEvidenceCertain(HistoricalFact fact)
    {
        HistoricalDate? boundary = fact.Period.Start;
        return fact.State == HistoricalFactState.Verified
            && fact.Period.IsPoint
            && fact.Period.StartConfidence == PeriodBoundaryConfidence.Confirmed
            && fact.Period.EndConfidence == PeriodBoundaryConfidence.Confirmed
            && boundary is not null
            && !boundary.IsApproximate
            && !boundary.Qualifier.HasValue;
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
