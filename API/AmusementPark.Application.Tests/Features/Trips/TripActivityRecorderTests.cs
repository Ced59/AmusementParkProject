using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripActivityRecorderTests
{
    [Fact]
    public void IdempotentOperationKey_ShouldBeStableAndBoundedForTheLongestClientKey()
    {
        string operationId = new('x', TripPlanLifecycleService.MaximumIdempotencyKeyLength);

        string first = TripActivityRecorder.IdempotentOperationKey(
            TripActivityKind.TripCreated,
            operationId);
        string second = TripActivityRecorder.IdempotentOperationKey(
            TripActivityKind.TripCreated,
            operationId);

        Assert.Equal(first, second);
        Assert.InRange(first.Length, 1, TripActivityEvent.MaximumOperationKeyLength);
        Assert.DoesNotContain(operationId, first, StringComparison.Ordinal);
    }

    [Fact]
    public async Task RecordAsync_ShouldSnapshotTheActorsRoleAndUseTheInjectedClock()
    {
        DateTime nowUtc = new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "owner-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            nowUtc);
        Mock<ITripAuditWriter> writer = new Mock<ITripAuditWriter>(MockBehavior.Strict);
        writer.Setup(port => port.AppendAsync(
                It.Is<TripActivityWrite>(write =>
                    write.TripPlanId == trip.Id
                    && write.ActorRole == TripEffectiveRole.Owner
                    && write.Kind == TripActivityKind.TripRenamed
                    && write.OccurredAtUtc == nowUtc),
                CancellationToken.None))
            .ReturnsAsync(true);
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripActivityRecorder recorder = new TripActivityRecorder(writer.Object, timeProvider.Object);

        await recorder.RecordAsync(
            trip,
            trip.OwnerUserId,
            TripActivityKind.TripRenamed,
            "root:TripRenamed:2",
            1,
            CancellationToken.None);

        writer.VerifyAll();
        timeProvider.VerifyAll();
    }

    [Fact]
    public async Task PublishReadOnlyAsync_ShouldUseTheDedicatedDurableAuditPath()
    {
        TripActivityWrite activity = new(
            TripPlanId.New(),
            TripMemberId.New(),
            TripEffectiveRole.Participant,
            TripActivityKind.PlanExported,
            "idempotent:PlanExported:hash",
            1,
            new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc));
        Mock<ITripAuditWriter> writer = new(MockBehavior.Strict);
        writer.Setup(port => port.AppendReadOnlyAsync(activity, CancellationToken.None))
            .ReturnsAsync(true);
        TripActivityRecorder recorder = new(writer.Object);

        bool result = await recorder.PublishReadOnlyAsync(activity, CancellationToken.None);

        Assert.True(result);
        writer.VerifyAll();
    }

    [Fact]
    public void ChildOperationKey_ShouldDistinguishTwoLeasesWithTheSameOperationId()
    {
        TripMemberId actorMemberId = TripMemberId.Parse("member-1");
        TripChildMutationLease first = new TripChildMutationLease(
            "same-content",
            actorMemberId,
            1,
            8,
            new DateTime(2027, 3, 4, 10, 1, 0, DateTimeKind.Utc));
        TripChildMutationLease second = new TripChildMutationLease(
            "same-content",
            actorMemberId,
            1,
            10,
            new DateTime(2027, 3, 4, 10, 2, 0, DateTimeKind.Utc));

        string firstKey = TripActivityRecorder.ChildOperationKey(
            TripActivityKind.DayUpdated,
            first);
        string secondKey = TripActivityRecorder.ChildOperationKey(
            TripActivityKind.DayUpdated,
            second);

        Assert.NotEqual(firstKey, secondKey);
        Assert.Contains(":8", firstKey, StringComparison.Ordinal);
        Assert.Contains(":10", secondKey, StringComparison.Ordinal);
    }
}
