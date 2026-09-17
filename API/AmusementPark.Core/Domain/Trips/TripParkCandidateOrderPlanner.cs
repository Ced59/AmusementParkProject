namespace AmusementPark.Core.Domain.Trips;

public static class TripParkCandidateOrderPlanner
{
    public static long AllocateAppend(long? currentMaximum)
    {
        return currentMaximum.HasValue
            ? checked(currentMaximum.Value + TripParkCandidate.SortPositionStep)
            : TripParkCandidate.SortPositionStep;
    }

    public static TripParkCandidateOrderPlan PlanMove(
        IReadOnlyCollection<TripParkCandidate> candidates,
        TripParkCandidateId movedCandidateId,
        TripParkCandidateId? anchorCandidateId,
        TripParkCandidatePlacement placement)
    {
        ArgumentNullException.ThrowIfNull(candidates);
        if (candidates.Count is < 1 or > TripParkCandidate.MaximumCandidatesPerTrip)
        {
            throw new ArgumentOutOfRangeException(nameof(candidates));
        }

        if (!Enum.IsDefined(placement))
        {
            throw new ArgumentOutOfRangeException(nameof(placement));
        }

        bool requiresAnchor = placement is TripParkCandidatePlacement.Before
            or TripParkCandidatePlacement.After;
        if (requiresAnchor != anchorCandidateId.HasValue)
        {
            throw new ArgumentException(
                "Before and after require an anchor, while first and last forbid one.",
                nameof(anchorCandidateId));
        }

        List<TripParkCandidate> original = candidates
            .OrderBy(static candidate => candidate.SortPosition)
            .ThenBy(static candidate => candidate.CreatedAtUtc)
            .ThenBy(static candidate => candidate.Id.Value, StringComparer.Ordinal)
            .ToList();
        EnsureDistinctIds(original);
        TripParkCandidate? moved = original.FirstOrDefault(candidate => candidate.Id == movedCandidateId);
        if (moved is null)
        {
            throw new KeyNotFoundException("The moved candidate is not present.");
        }

        List<TripParkCandidate> reordered = original
            .Where(candidate => candidate.Id != movedCandidateId)
            .ToList();
        int insertionIndex = ResolveInsertionIndex(reordered, anchorCandidateId, placement);
        reordered.Insert(insertionIndex, moved);
        IReadOnlyCollection<TripParkCandidateOrderGuard> guards = BuildGuards(original);
        if (original.Select(static candidate => candidate.Id)
            .SequenceEqual(reordered.Select(static candidate => candidate.Id)))
        {
            return new TripParkCandidateOrderPlan(
                Array.Empty<TripParkCandidateOrderPosition>(),
                guards,
                false);
        }

        long? directPosition = TryResolveDirectPosition(reordered, insertionIndex);
        if (directPosition.HasValue)
        {
            return new TripParkCandidateOrderPlan(
                new[]
                {
                    new TripParkCandidateOrderPosition(
                        moved.Id,
                        directPosition.Value,
                        moved.Version),
                },
                guards,
                false);
        }

        TripParkCandidateOrderPosition[] changes = reordered
            .Select(static (candidate, index) => new TripParkCandidateOrderPosition(
                candidate.Id,
                checked((index + 1L) * TripParkCandidate.SortPositionStep),
                candidate.Version))
            .Where(change => original.Single(candidate => candidate.Id == change.CandidateId).SortPosition
                != change.SortPosition)
            .ToArray();
        return new TripParkCandidateOrderPlan(changes, guards, true);
    }

    private static int ResolveInsertionIndex(
        IReadOnlyList<TripParkCandidate> candidates,
        TripParkCandidateId? anchorCandidateId,
        TripParkCandidatePlacement placement)
    {
        if (placement == TripParkCandidatePlacement.First)
        {
            return 0;
        }

        if (placement == TripParkCandidatePlacement.Last)
        {
            return candidates.Count;
        }

        int anchorIndex = candidates
            .Select(static (candidate, index) => new { candidate.Id, Index = index })
            .Where(item => item.Id == anchorCandidateId!.Value)
            .Select(static item => item.Index)
            .DefaultIfEmpty(-1)
            .Single();
        if (anchorIndex < 0)
        {
            throw new KeyNotFoundException("The anchor candidate is not present.");
        }

        return placement == TripParkCandidatePlacement.Before ? anchorIndex : anchorIndex + 1;
    }

    private static long? TryResolveDirectPosition(
        IReadOnlyList<TripParkCandidate> candidates,
        int movedIndex)
    {
        TripParkCandidate? previous = movedIndex > 0 ? candidates[movedIndex - 1] : null;
        TripParkCandidate? next = movedIndex < candidates.Count - 1 ? candidates[movedIndex + 1] : null;
        if (previous is null && next is null)
        {
            return TripParkCandidate.SortPositionStep;
        }

        if (previous is null)
        {
            return next!.SortPosition >= long.MinValue + TripParkCandidate.SortPositionStep
                ? next.SortPosition - TripParkCandidate.SortPositionStep
                : null;
        }

        if (next is null)
        {
            return previous.SortPosition <= long.MaxValue - TripParkCandidate.SortPositionStep
                ? previous.SortPosition + TripParkCandidate.SortPositionStep
                : null;
        }

        ulong gap = unchecked((ulong)(next.SortPosition - previous.SortPosition));
        long midpoint = (previous.SortPosition & next.SortPosition)
            + ((previous.SortPosition ^ next.SortPosition) >> 1);
        return gap > 1 ? midpoint : null;
    }

    private static IReadOnlyCollection<TripParkCandidateOrderGuard> BuildGuards(
        IReadOnlyCollection<TripParkCandidate> candidates)
    {
        return candidates.Select(static candidate => new TripParkCandidateOrderGuard(
            candidate.Id,
            candidate.SortPosition,
            candidate.Version)).ToArray();
    }

    private static void EnsureDistinctIds(IReadOnlyCollection<TripParkCandidate> candidates)
    {
        if (candidates.Select(static candidate => candidate.Id).Distinct().Count() != candidates.Count)
        {
            throw new ArgumentException(
                "The candidate order cannot contain duplicate identifiers.",
                nameof(candidates));
        }
    }
}
