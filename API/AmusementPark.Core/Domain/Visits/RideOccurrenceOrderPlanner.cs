namespace AmusementPark.Core.Domain.Visits;

public enum RideOccurrencePlacement
{
    First = 1,
    Last = 2,
    Before = 3,
    After = 4,
}

public sealed record RideOccurrenceOrderPosition(
    RideOccurrenceId OccurrenceId,
    long SortPosition);

public sealed record RideOccurrenceOrderGuard(
    RideOccurrenceId OccurrenceId,
    long SortPosition);

public sealed record RideOccurrenceOrderPlan(
    IReadOnlyCollection<RideOccurrenceOrderPosition> Changes,
    IReadOnlyCollection<RideOccurrenceOrderGuard> Guards,
    bool WasNormalized);

/// <summary>
/// Calcule les positions techniques sans faire dépendre l'ordre visible d'un rang persisté.
/// </summary>
public static class RideOccurrenceOrderPlanner
{
    public const int MaximumReorderSize = 2000;

    public static IReadOnlyList<long> AllocateAppend(long? currentMaximum, int count)
    {
        if (count is < 1 or > 100)
        {
            throw new ArgumentOutOfRangeException(nameof(count));
        }

        long first = currentMaximum.HasValue
            ? checked(currentMaximum.Value + RideOccurrence.SortPositionStep)
            : RideOccurrence.SortPositionStep;
        long last = checked(first + ((count - 1L) * RideOccurrence.SortPositionStep));
        _ = last;

        long[] positions = new long[count];
        for (int index = 0; index < count; index++)
        {
            positions[index] = checked(first + (index * RideOccurrence.SortPositionStep));
        }

        return positions;
    }

    public static RideOccurrenceOrderPlan PlanMove(
        IReadOnlyList<RideOccurrence> orderedOccurrences,
        RideOccurrenceId movedOccurrenceId,
        RideOccurrenceId? anchorOccurrenceId,
        RideOccurrencePlacement placement)
    {
        ArgumentNullException.ThrowIfNull(orderedOccurrences);
        if (orderedOccurrences.Count is < 1 or > MaximumReorderSize)
        {
            throw new ArgumentOutOfRangeException(nameof(orderedOccurrences));
        }

        if (!Enum.IsDefined(placement))
        {
            throw new ArgumentOutOfRangeException(nameof(placement));
        }

        bool requiresAnchor = placement is RideOccurrencePlacement.Before
            or RideOccurrencePlacement.After;
        if (requiresAnchor != anchorOccurrenceId.HasValue)
        {
            throw new ArgumentException(
                "Before and after require an anchor, while first and last forbid one.",
                nameof(anchorOccurrenceId));
        }

        List<RideOccurrence> original = orderedOccurrences
            .OrderBy(static occurrence => occurrence.SortPosition)
            .ThenBy(static occurrence => occurrence.CreatedAtUtc)
            .ThenBy(static occurrence => occurrence.Id.Value, StringComparer.Ordinal)
            .ToList();
        EnsureDistinctIds(original);
        RideOccurrence? moved = original.FirstOrDefault(
            occurrence => occurrence.Id == movedOccurrenceId);
        if (moved is null)
        {
            throw new KeyNotFoundException("The moved ride occurrence is not present.");
        }

        List<RideOccurrence> reordered = original
            .Where(occurrence => occurrence.Id != movedOccurrenceId)
            .ToList();
        int insertionIndex = ResolveInsertionIndex(
            reordered,
            anchorOccurrenceId,
            placement);
        reordered.Insert(insertionIndex, moved);

        if (original.Select(static occurrence => occurrence.Id)
            .SequenceEqual(reordered.Select(static occurrence => occurrence.Id)))
        {
            return new RideOccurrenceOrderPlan(
                Array.Empty<RideOccurrenceOrderPosition>(),
                BuildGuards(original),
                false);
        }

        long? directPosition = TryResolveDirectPosition(reordered, insertionIndex);
        if (directPosition.HasValue)
        {
            return new RideOccurrenceOrderPlan(
                new[]
                {
                    new RideOccurrenceOrderPosition(movedOccurrenceId, directPosition.Value),
                },
                BuildGuards(original),
                false);
        }

        List<RideOccurrenceOrderPosition> changes = new List<RideOccurrenceOrderPosition>();
        for (int index = 0; index < reordered.Count; index++)
        {
            long normalizedPosition = checked(
                (index + 1L) * RideOccurrence.SortPositionStep);
            if (reordered[index].SortPosition != normalizedPosition)
            {
                changes.Add(new RideOccurrenceOrderPosition(
                    reordered[index].Id,
                    normalizedPosition));
            }
        }

        return new RideOccurrenceOrderPlan(
            changes,
            BuildGuards(original),
            true);
    }

    public static RideOccurrenceOrderPlan PlanNormalization(
        IReadOnlyList<RideOccurrence> orderedOccurrences)
    {
        ArgumentNullException.ThrowIfNull(orderedOccurrences);
        if (orderedOccurrences.Count is < 1 or > MaximumReorderSize)
        {
            throw new ArgumentOutOfRangeException(nameof(orderedOccurrences));
        }

        List<RideOccurrence> ordered = orderedOccurrences
            .OrderBy(static occurrence => occurrence.SortPosition)
            .ThenBy(static occurrence => occurrence.CreatedAtUtc)
            .ThenBy(static occurrence => occurrence.Id.Value, StringComparer.Ordinal)
            .ToList();
        EnsureDistinctIds(ordered);
        List<RideOccurrenceOrderPosition> changes = new List<RideOccurrenceOrderPosition>();
        for (int index = 0; index < ordered.Count; index++)
        {
            long normalizedPosition = checked(
                (index + 1L) * RideOccurrence.SortPositionStep);
            if (ordered[index].SortPosition != normalizedPosition)
            {
                changes.Add(new RideOccurrenceOrderPosition(
                    ordered[index].Id,
                    normalizedPosition));
            }
        }

        return new RideOccurrenceOrderPlan(
            changes,
            BuildGuards(ordered),
            true);
    }

    private static int ResolveInsertionIndex(
        IReadOnlyList<RideOccurrence> occurrencesWithoutMoved,
        RideOccurrenceId? anchorOccurrenceId,
        RideOccurrencePlacement placement)
    {
        if (placement == RideOccurrencePlacement.First)
        {
            return 0;
        }

        if (placement == RideOccurrencePlacement.Last)
        {
            return occurrencesWithoutMoved.Count;
        }

        int anchorIndex = occurrencesWithoutMoved
            .Select(static (occurrence, index) => new { occurrence.Id, Index = index })
            .Where(item => item.Id == anchorOccurrenceId!.Value)
            .Select(static item => item.Index)
            .DefaultIfEmpty(-1)
            .Single();
        if (anchorIndex < 0)
        {
            throw new KeyNotFoundException("The anchor ride occurrence is not present.");
        }

        return placement == RideOccurrencePlacement.Before
            ? anchorIndex
            : anchorIndex + 1;
    }

    private static long? TryResolveDirectPosition(
        IReadOnlyList<RideOccurrence> reordered,
        int movedIndex)
    {
        RideOccurrence? previous = movedIndex > 0 ? reordered[movedIndex - 1] : null;
        RideOccurrence? next = movedIndex < reordered.Count - 1
            ? reordered[movedIndex + 1]
            : null;

        if (previous is null && next is null)
        {
            return RideOccurrence.SortPositionStep;
        }

        if (previous is null)
        {
            return next!.SortPosition >= long.MinValue + RideOccurrence.SortPositionStep
                ? next.SortPosition - RideOccurrence.SortPositionStep
                : null;
        }

        if (next is null)
        {
            return previous.SortPosition <= long.MaxValue - RideOccurrence.SortPositionStep
                ? previous.SortPosition + RideOccurrence.SortPositionStep
                : null;
        }

        ulong gap = unchecked((ulong)(next.SortPosition - previous.SortPosition));
        long midpoint = (previous.SortPosition & next.SortPosition)
            + ((previous.SortPosition ^ next.SortPosition) >> 1);
        return gap > 1
            ? midpoint
            : null;
    }

    private static IReadOnlyCollection<RideOccurrenceOrderGuard> BuildGuards(
        IReadOnlyCollection<RideOccurrence> occurrences)
    {
        return occurrences
            .Select(static occurrence => new RideOccurrenceOrderGuard(
                occurrence.Id,
                occurrence.SortPosition))
            .ToArray();
    }

    private static void EnsureDistinctIds(IReadOnlyCollection<RideOccurrence> occurrences)
    {
        if (occurrences.Select(static occurrence => occurrence.Id).Distinct().Count()
            != occurrences.Count)
        {
            throw new ArgumentException(
                "The ride occurrence order cannot contain duplicate identifiers.",
                nameof(occurrences));
        }
    }
}
