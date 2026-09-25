using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripActivityServiceTests
{
    [Fact]
    public async Task GetAsync_ShouldResolveOnlyActiveMemberNamesAndReturnAnOpaqueCursor()
    {
        DateTime nowUtc = new DateTime(2027, 3, 4, 10, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "owner-1",
            "Voyage privé",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripActivityEvent[] activities = Enumerable.Range(1, TripActivityService.PageSize + 1)
            .Select(index => new TripActivityEvent(
                $"activity-{index}",
                trip.Id,
                index == 2 ? TripMemberId.Parse("member-departed") : owner.Id,
                index == 2 ? TripEffectiveRole.Participant : TripEffectiveRole.Owner,
                index == 2 ? TripActivityKind.ParticipantLeft : TripActivityKind.TripRenamed,
                $"operation-{index}",
                TripActivityService.PageSize + 2 - index,
                1,
                nowUtc.AddMinutes(-index)))
            .ToArray();
        Mock<ITripPlanRepository> plans = new Mock<ITripPlanRepository>(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                trip.OwnerUserId,
                trip.Id,
                CancellationToken.None))
            .ReturnsAsync(trip);
        Mock<ITripAuditReader> audit = new Mock<ITripAuditReader>(MockBehavior.Strict);
        audit.Setup(reader => reader.ListAsync(
                trip.Id,
                null,
                TripActivityService.PageSize + 1,
                CancellationToken.None))
            .ReturnsAsync(activities);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { trip.OwnerUserId })),
                CancellationToken.None))
            .ReturnsAsync(new[] { new User { Id = trip.OwnerUserId, PublicDisplayName = "Camille" } });
        TripActivityService service = new TripActivityService(plans.Object, audit.Object, users.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripActivityPageResult> result =
            await service.GetAsync(trip.OwnerUserId, trip.Id.Value, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripActivityPageResult page = Assert.IsType<TripActivityPageResult>(result.Value);
        Assert.Equal(TripActivityService.PageSize, page.Entries.Count);
        Assert.Equal(2, page.NextBeforeSequence);
        TripActivityEntryResult departed = page.Entries.Single(entry => entry.Kind == TripActivityKind.ParticipantLeft);
        Assert.Equal("—", departed.ActorDisplayName);
        Assert.All(
            page.Entries.Where(entry => entry.Kind == TripActivityKind.TripRenamed),
            entry => Assert.Equal("Camille", entry.ActorDisplayName));
        plans.VerifyAll();
        audit.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task GetAsync_WhenTripIsNotAccessible_ShouldNotReadAuditEvents()
    {
        TripPlanId tripId = TripPlanId.Parse("trip-1");
        Mock<ITripPlanRepository> plans = new Mock<ITripPlanRepository>(MockBehavior.Strict);
        plans.Setup(repository => repository.GetAccessibleAsync(
                "outsider-1",
                tripId,
                CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        Mock<ITripAuditReader> audit = new Mock<ITripAuditReader>(MockBehavior.Strict);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        TripActivityService service = new TripActivityService(plans.Object, audit.Object, users.Object);

        AmusementPark.Application.Errors.ApplicationResult<TripActivityPageResult> result =
            await service.GetAsync("outsider-1", tripId.Value, null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        audit.VerifyNoOtherCalls();
        users.VerifyNoOtherCalls();
    }
}
