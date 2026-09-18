using AmusementPark.Core.Domain.Trips;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Trips;

public sealed class TripPreferenceAggregateTests
{
    [Fact]
    public void Create_WhenOneMemberObjects_ShouldExposeConflictInsteadOfMajority()
    {
        TripPreferenceAggregate aggregate = TripPreferenceAggregate.Create(4, 2, 1, 0, 1);

        Assert.Equal(TripPreferenceCompatibility.Conflict, aggregate.Compatibility);
        Assert.True(aggregate.HasIndividualConstraint);
        Assert.False(aggregate.IsGroupPriority);
        Assert.True(aggregate.IsCompatibilityKnown);
    }

    [Fact]
    public void Create_WhenEveryoneAnswersPositively_ShouldExposeConsensus()
    {
        TripPreferenceAggregate aggregate = TripPreferenceAggregate.Create(4, 1, 2, 1, 0);

        Assert.Equal(TripPreferenceCompatibility.Consensus, aggregate.Compatibility);
        Assert.Equal(0, aggregate.UnansweredCount);
        Assert.True(aggregate.IsGroupPriority);
    }

    [Fact]
    public void Create_WhenAnswersAreMissing_ShouldKeepCompatibilityProvisional()
    {
        TripPreferenceAggregate aggregate = TripPreferenceAggregate.Create(4, 1, 1, 0, 0);

        Assert.Equal(TripPreferenceCompatibility.Mixed, aggregate.Compatibility);
        Assert.Equal(2, aggregate.UnansweredCount);
        Assert.False(aggregate.IsCompatibilityKnown);
    }
}
