using AmusementPark.Core.Domain.Visits;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Visits;

public sealed class RideOccurrenceOrderPlannerTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 3, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void AllocateAppend_AcrossTenBoundedBatches_ShouldAllocateOneThousandStablePositions()
    {
        List<long> positions = new List<long>();
        long? currentMaximum = null;

        for (int batch = 0; batch < 10; batch++)
        {
            IReadOnlyList<long> allocated =
                RideOccurrenceOrderPlanner.AllocateAppend(currentMaximum, 100);
            positions.AddRange(allocated);
            currentMaximum = allocated[^1];
        }

        Assert.Equal(1000, positions.Count);
        Assert.Equal(1024, positions[0]);
        Assert.Equal(1024000, positions[^1]);
        Assert.Equal(positions.Count, positions.Distinct().Count());
    }

    [Fact]
    public void PlanMove_WithAvailableGap_ShouldOnlyMoveTheRequestedOccurrence()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("one", 1024),
            CreateOccurrence("two", 2048),
            CreateOccurrence("three", 3072),
        };

        RideOccurrenceOrderPlan plan = RideOccurrenceOrderPlanner.PlanMove(
            occurrences,
            RideOccurrenceId.Parse("three"),
            RideOccurrenceId.Parse("two"),
            RideOccurrencePlacement.Before);

        RideOccurrenceOrderPosition change = Assert.Single(plan.Changes);
        Assert.Equal("three", change.OccurrenceId.Value);
        Assert.Equal(1536, change.SortPosition);
        Assert.Equal(
            new[] { "one", "two", "three" },
            plan.Guards.Select(static guard => guard.OccurrenceId.Value));
        Assert.False(plan.WasNormalized);
    }

    [Fact]
    public void PlanMove_WhenGapIsExhausted_ShouldNormalizeOnlyTheProvidedVisit()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("one", 1024),
            CreateOccurrence("two", 1025),
            CreateOccurrence("three", 4096),
        };

        RideOccurrenceOrderPlan plan = RideOccurrenceOrderPlanner.PlanMove(
            occurrences,
            RideOccurrenceId.Parse("three"),
            RideOccurrenceId.Parse("two"),
            RideOccurrencePlacement.Before);

        Assert.True(plan.WasNormalized);
        Assert.Equal(
            new[] { 2048L, 3072L },
            plan.Changes.Select(static change => change.SortPosition));
        Assert.Equal(
            new[] { "three", "two" },
            plan.Changes.Select(static change => change.OccurrenceId.Value));
    }

    [Fact]
    public void PlanMove_AcrossTheWholeLongRange_ShouldUseAnOverflowSafeMidpoint()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("one", long.MinValue),
            CreateOccurrence("two", long.MaxValue - 1),
            CreateOccurrence("three", long.MaxValue),
        };

        RideOccurrenceOrderPlan plan = RideOccurrenceOrderPlanner.PlanMove(
            occurrences,
            RideOccurrenceId.Parse("three"),
            RideOccurrenceId.Parse("two"),
            RideOccurrencePlacement.Before);

        RideOccurrenceOrderPosition change = Assert.Single(plan.Changes);
        Assert.Equal(-1, change.SortPosition);
    }

    [Fact]
    public void PlanMove_WhenOrderDoesNotChange_ShouldBeANoOp()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("one", 1024),
            CreateOccurrence("two", 2048),
        };

        RideOccurrenceOrderPlan plan = RideOccurrenceOrderPlanner.PlanMove(
            occurrences,
            RideOccurrenceId.Parse("one"),
            null,
            RideOccurrencePlacement.First);

        Assert.Empty(plan.Changes);
        Assert.Equal(
            new[] { "one", "two" },
            plan.Guards.Select(static guard => guard.OccurrenceId.Value));
        Assert.False(plan.WasNormalized);
    }

    [Fact]
    public void PlanNormalization_NearLongMaximum_ShouldRestoreAppendCapacity()
    {
        IReadOnlyList<RideOccurrence> occurrences = new[]
        {
            CreateOccurrence("one", long.MaxValue - 1),
            CreateOccurrence("two", long.MaxValue),
        };

        Assert.Throws<OverflowException>(() =>
            RideOccurrenceOrderPlanner.AllocateAppend(long.MaxValue, 1));

        RideOccurrenceOrderPlan plan =
            RideOccurrenceOrderPlanner.PlanNormalization(occurrences);

        Assert.True(plan.WasNormalized);
        Assert.Equal(
            new[] { 1024L, 2048L },
            plan.Changes.Select(static change => change.SortPosition));
        Assert.Equal(3072, RideOccurrenceOrderPlanner.AllocateAppend(2048, 1)[0]);
    }

    [Theory]
    [InlineData(1997, 1995, 2000, HistoricalConsistency.Verified)]
    [InlineData(1980, 1995, 2000, HistoricalConsistency.ConfirmedConflict)]
    [InlineData(2010, 1995, 2000, HistoricalConsistency.ConfirmedConflict)]
    public void HistoricalConsistency_ForExactDay_ShouldRespectKnownBounds(
        int visitYear,
        int openingYear,
        int closingYear,
        HistoricalConsistency expected)
    {
        HistoricalConsistency result =
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                VisitDate.ForDay(visitYear, 6, 15),
                new DateOnly(openingYear, 1, 1),
                new DateOnly(closingYear, 12, 31));

        Assert.Equal(expected, result);
    }

    [Fact]
    public void HistoricalConsistency_ForPartiallyOverlappingYear_ShouldRemainUnverified()
    {
        HistoricalConsistency result =
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                VisitDate.ForYear(2000),
                new DateOnly(2000, 7, 1),
                null);

        Assert.Equal(HistoricalConsistency.Unverified, result);
    }

    [Fact]
    public void HistoricalConsistency_WithoutKnownBounds_ShouldRemainUnverified()
    {
        HistoricalConsistency result =
            RideOccurrenceHistoricalConsistencyEvaluator.Evaluate(
                VisitDate.ForDay(2000, 1, 1),
                null,
                null);

        Assert.Equal(HistoricalConsistency.Unverified, result);
    }

    private static RideOccurrence CreateOccurrence(string id, long position)
    {
        Visit visit = Visit.Create(
            VisitId.Parse("visit-1"),
            "user-1",
            "park-1",
            VisitDate.ForDay(2026, 9, 3),
            "Europe/Paris",
            LocalServiceDayConvention.VisitStartLocalDate,
            null,
            null,
            NowUtc);
        return RideOccurrence.Create(
            RideOccurrenceId.Parse(id),
            visit,
            $"item-{id}",
            position,
            new OccurrenceMoment(null, false),
            RideOccurrenceStatus.Completed,
            RideLogSource.Manual,
            HistoricalConsistency.Verified,
            null,
            null,
            NowUtc);
    }
}
