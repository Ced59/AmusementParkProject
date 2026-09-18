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
            .ReturnsAsync((TripActivityWrite write, CancellationToken _) => new TripActivityEvent(
                "activity-1",
                write.TripPlanId,
                write.ActorMemberId,
                write.ActorRole,
                write.Kind,
                write.OperationKey,
                1,
                write.AffectedCount,
                write.OccurredAtUtc));
        Mock<TimeProvider> timeProvider = new Mock<TimeProvider>(MockBehavior.Strict);
        timeProvider.Setup(provider => provider.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripActivityRecorder recorder = new TripActivityRecorder(writer.Object, timeProvider.Object);

        TripActivityEvent activity = await recorder.RecordAsync(
            trip,
            trip.OwnerUserId,
            TripActivityKind.TripRenamed,
            "root:TripRenamed:2",
            1,
            CancellationToken.None);

        Assert.Equal(TripEffectiveRole.Owner, activity.ActorRole);
        writer.VerifyAll();
        timeProvider.VerifyAll();
    }
}
