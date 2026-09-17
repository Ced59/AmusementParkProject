using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripParkCandidateOrderPlannerTests
{
    private static readonly DateTime CreatedAtUtc = new(2027, 2, 3, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void PlanMove_WhenGapExists_ShouldOnlyMoveTheRequestedCandidate()
    {
        TripParkCandidate first = CreateCandidate("park-1", 1024, 0);
        TripParkCandidate second = CreateCandidate("park-2", 2048, 1);
        TripParkCandidate third = CreateCandidate("park-3", 3072, 2);

        TripParkCandidateOrderPlan plan = TripParkCandidateOrderPlanner.PlanMove(
            new[] { first, second, third },
            third.Id,
            second.Id,
            TripParkCandidatePlacement.Before);

        TripParkCandidateOrderPosition change = Assert.Single(plan.Changes);
        Assert.Equal(third.Id, change.CandidateId);
        Assert.Equal(1536, change.SortPosition);
        Assert.False(plan.WasRenormalized);
        Assert.Equal(3, plan.Guards.Count);
    }

    [Fact]
    public void PlanMove_WhenNoGapExists_ShouldRenormalizeTheBoundedList()
    {
        TripParkCandidate first = CreateCandidate("park-1", 1, 0);
        TripParkCandidate second = CreateCandidate("park-2", 2, 1);
        TripParkCandidate third = CreateCandidate("park-3", 3, 2);

        TripParkCandidateOrderPlan plan = TripParkCandidateOrderPlanner.PlanMove(
            new[] { first, second, third },
            third.Id,
            second.Id,
            TripParkCandidatePlacement.Before);

        Assert.True(plan.WasRenormalized);
        Assert.Equal(3, plan.Changes.Count);
        Assert.Equal(
            new long[] { 1024, 2048, 3072 },
            plan.Changes.Select(static change => change.SortPosition));
    }

    private static TripParkCandidate CreateCandidate(string parkId, long position, int minuteOffset)
    {
        return TripParkCandidate.Create(
            TripParkCandidateId.New(),
            TripPlanId.Parse("trip-1"),
            parkId,
            Array.Empty<DateOnly>(),
            TripParkCandidateSource.Manual,
            null,
            null,
            TripMemberId.Parse("member-1"),
            position,
            CreatedAtUtc.AddMinutes(minuteOffset));
    }
}
