using AmusementPark.Core.Domain.History;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.History;

public sealed class HistoricalLineageCycleDetectorTests
{
    [Fact]
    public void HasDirectedCycle_WhenDirectedRelationsReturnToStart_ShouldReturnTrue()
    {
        HistoricalRelation first = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-1",
            targetId: "item-2");
        HistoricalRelation second = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-2",
            targetId: "item-1");

        Assert.True(HistoricalLineageCycleDetector.HasDirectedCycle(new[] { first, second }));
    }

    [Fact]
    public void HasDirectedCycle_WhenDirectedLineageIsAcyclic_ShouldReturnFalse()
    {
        HistoricalRelation first = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-1",
            targetId: "item-2");
        HistoricalRelation second = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-2",
            targetId: "item-3");

        Assert.False(HistoricalLineageCycleDetector.HasDirectedCycle(new[] { first, second }));
    }

    [Fact]
    public void HasDirectedCycle_WhenRelationIsSymmetric_ShouldIgnoreBothWayTraversal()
    {
        HistoricalRelation relation = HistoricalRelationTests.CreatePublishedRelation(
            sourceId: "item-1",
            targetId: "item-2",
            type: HistoricalRelationType.SamePhysicalAssetAs,
            direction: HistoricalRelationDirection.Symmetric);

        Assert.False(HistoricalLineageCycleDetector.HasDirectedCycle(new[] { relation }));
    }
}
