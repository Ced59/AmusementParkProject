using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Core.Domain.Trips;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripNotificationServiceTests
{
    [Fact]
    public async Task GetAsync_WhenNoSubscriptionExists_ShouldReturnOptInDisabled()
    {
        TripPlan trip = CreateTrip();
        Mock<ITripPlanRepository> plans = AccessiblePlans(trip);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync((TripNotificationSubscription?)null);
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.GetAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            CancellationToken.None);

        TripNotificationStateResult state = Assert.IsType<TripNotificationStateResult>(result.Value);
        Assert.False(state.Enabled);
        Assert.Equal(0, state.Version);
        Assert.Equal(0, state.UnreadCount);
        audit.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldStartAfterExistingActivity()
    {
        TripPlan trip = CreateTrip();
        TripMember owner = Assert.Single(trip.Members);
        DateTime baselineUtc = new(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        Mock<ITripPlanRepository> plans = AccessiblePlans(trip);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.GetLatestSequenceAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(8);
        audit.Setup(reader => reader.ListImportantAfterAsync(
                trip.Id,
                owner.Id,
                8,
                baselineUtc,
                TripNotificationPolicy.MaximumUnreadCount + 1,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripActivityEvent>());
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync((TripNotificationSubscription?)null);
        TripNotificationSubscription? created = null;
        subscriptions.Setup(repository => repository.CreateAsync(
                It.IsAny<TripNotificationSubscription>(),
                CancellationToken.None))
            .Callback<TripNotificationSubscription, CancellationToken>((value, _) => created = value)
            .ReturnsAsync(true);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(new DateTimeOffset(baselineUtc));
        TripNotificationService service = new(
            plans.Object,
            audit.Object,
            subscriptions.Object,
            clock.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            true,
            0,
            CancellationToken.None);

        Assert.True(Assert.IsType<TripNotificationStateResult>(result.Value).Enabled);
        Assert.Equal(8, Assert.IsType<TripNotificationSubscription>(created).SeenThroughSequence);
        Assert.Equal(baselineUtc, created.UpdatedAtUtc);
        subscriptions.VerifyAll();
        audit.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenMemberLeavesDuringCreation_ShouldCompensateSubscription()
    {
        TripPlan trip = CreateTrip();
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.SetupSequence(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip)
            .ReturnsAsync((TripPlan?)null);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.GetLatestSequenceAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(8);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync((TripNotificationSubscription?)null);
        subscriptions.Setup(repository => repository.CreateAsync(
                It.IsAny<TripNotificationSubscription>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        subscriptions.Setup(repository => repository.DeleteForMemberAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            true,
            0,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        subscriptions.VerifyAll();
        plans.VerifyAll();
        audit.VerifyAll();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenRequestIsCancelledAfterCreation_ShouldStillCompensateDeparture()
    {
        using CancellationTokenSource cancellation = new();
        TripPlan trip = CreateTrip();
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                cancellation.Token))
            .ReturnsAsync(trip);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.GetLatestSequenceAsync(trip.Id, cancellation.Token))
            .ReturnsAsync(8);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                cancellation.Token))
            .ReturnsAsync((TripNotificationSubscription?)null);
        subscriptions.Setup(repository => repository.CreateAsync(
                It.IsAny<TripNotificationSubscription>(),
                cancellation.Token))
            .Callback(cancellation.Cancel)
            .ReturnsAsync(true);
        subscriptions.Setup(repository => repository.DeleteForMemberAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            true,
            0,
            cancellation.Token);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        subscriptions.VerifyAll();
        plans.VerifyAll();
        audit.VerifyAll();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenCompensationFails_ShouldLeaveSubscriptionForReconciliation()
    {
        TripPlan trip = CreateTrip();
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.SetupSequence(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip)
            .ReturnsAsync((TripPlan?)null);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.GetLatestSequenceAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(8);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync((TripNotificationSubscription?)null);
        TripNotificationSubscription? created = null;
        subscriptions.Setup(repository => repository.CreateAsync(
                It.IsAny<TripNotificationSubscription>(),
                CancellationToken.None))
            .Callback<TripNotificationSubscription, CancellationToken>((value, _) => created = value)
            .ReturnsAsync(true);
        subscriptions.Setup(repository => repository.DeleteForMemberAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ThrowsAsync(new IOException("Transient compensation failure."));
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        await Assert.ThrowsAsync<IOException>(() => service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            true,
            0,
            CancellationToken.None));

        Assert.NotNull(created);
        subscriptions.VerifyAll();
        plans.VerifyAll();
        audit.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_ShouldCountOnlyRepositoryFilteredImportantChanges()
    {
        TripPlan trip = CreateTrip();
        TripMember owner = Assert.Single(trip.Members);
        TripNotificationSubscription subscription = TripNotificationSubscription.Restore(
            "subscription-1",
            trip.Id,
            owner.Id,
            trip.OwnerUserId,
            true,
            2,
            trip.CreatedAtUtc,
            trip.CreatedAtUtc,
            4);
        Mock<ITripPlanRepository> plans = AccessiblePlans(trip);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.ListImportantAfterAsync(
                trip.Id,
                owner.Id,
                2,
                subscription.UpdatedAtUtc,
                TripNotificationPolicy.MaximumUnreadCount + 1,
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                Activity(trip, 3, TripActivityKind.DatesChanged),
                Activity(trip, 4, TripActivityKind.DayUpdated),
            });
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(subscription);
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.GetAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            CancellationToken.None);

        Assert.Equal(2, Assert.IsType<TripNotificationStateResult>(result.Value).UnreadCount);
    }

    [Fact]
    public async Task SetEnabledAsync_ShouldReplaceSubscriptionFromAPreviousMembership()
    {
        TripPlan trip = CreateTrip();
        TripMember owner = Assert.Single(trip.Members);
        DateTime baselineUtc = new(2027, 4, 5, 10, 0, 0, DateTimeKind.Utc);
        TripNotificationSubscription stale = TripNotificationSubscription.Restore(
            "subscription-stale",
            trip.Id,
            TripMemberId.Parse("previous-membership"),
            trip.OwnerUserId,
            true,
            2,
            trip.CreatedAtUtc,
            trip.CreatedAtUtc,
            4);
        Mock<ITripPlanRepository> plans = AccessiblePlans(trip);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        audit.Setup(reader => reader.GetLatestSequenceAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(8);
        audit.Setup(reader => reader.ListImportantAfterAsync(
                trip.Id,
                owner.Id,
                8,
                baselineUtc,
                TripNotificationPolicy.MaximumUnreadCount + 1,
                CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripActivityEvent>());
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(stale);
        subscriptions.Setup(repository => repository.DeleteForMemberAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripNotificationSubscription? created = null;
        subscriptions.Setup(repository => repository.CreateAsync(
                It.IsAny<TripNotificationSubscription>(),
                CancellationToken.None))
            .Callback<TripNotificationSubscription, CancellationToken>((value, _) => created = value)
            .ReturnsAsync(true);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(provider => provider.GetUtcNow())
            .Returns(new DateTimeOffset(baselineUtc));
        TripNotificationService service = new(
            plans.Object,
            audit.Object,
            subscriptions.Object,
            clock.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            true,
            0,
            CancellationToken.None);

        Assert.True(Assert.IsType<TripNotificationStateResult>(result.Value).Enabled);
        Assert.Equal(owner.Id, Assert.IsType<TripNotificationSubscription>(created).MemberId);
        plans.VerifyAll();
        audit.VerifyAll();
        subscriptions.VerifyAll();
        clock.VerifyAll();
    }

    [Fact]
    public async Task SetEnabledAsync_WhenVersionIsStale_ShouldExposeCurrentVersionWithoutWriting()
    {
        TripPlan trip = CreateTrip();
        TripMember owner = Assert.Single(trip.Members);
        TripNotificationSubscription subscription = TripNotificationSubscription.Restore(
            "subscription-1",
            trip.Id,
            owner.Id,
            trip.OwnerUserId,
            true,
            2,
            trip.CreatedAtUtc,
            trip.CreatedAtUtc,
            4);
        Mock<ITripPlanRepository> plans = AccessiblePlans(trip);
        Mock<ITripAuditReader> audit = new(MockBehavior.Strict);
        Mock<ITripNotificationSubscriptionRepository> subscriptions = new(MockBehavior.Strict);
        subscriptions.Setup(repository => repository.GetAsync(
                trip.Id,
                trip.OwnerUserId,
                CancellationToken.None))
            .ReturnsAsync(subscription);
        TripNotificationService service = new(plans.Object, audit.Object, subscriptions.Object);

        ApplicationResult<TripNotificationStateResult> result = await service.SetEnabledAsync(
            trip.OwnerUserId,
            trip.Id.Value,
            false,
            3,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        ApplicationError error = Assert.Single(result.Errors);
        Assert.Equal("trip.notification.changed-concurrently", error.Code);
        Assert.Equal(4, error.CurrentVersion);
        audit.VerifyNoOtherCalls();
    }

    private static TripPlan CreateTrip()
    {
        return TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "owner-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            new DateTime(2027, 4, 5, 9, 0, 0, DateTimeKind.Utc));
    }

    private static Mock<ITripPlanRepository> AccessiblePlans(TripPlan trip)
    {
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        return plans;
    }

    private static TripActivityEvent Activity(
        TripPlan trip,
        long sequence,
        TripActivityKind kind)
    {
        return new TripActivityEvent(
            $"activity-{sequence}",
            trip.Id,
            null,
            null,
            kind,
            $"operation-{sequence}",
            sequence,
            1,
            trip.CreatedAtUtc.AddMinutes(sequence));
    }
}
