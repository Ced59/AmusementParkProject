using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripParticipantServiceTests
{
    private static readonly DateTime NowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ListAsync_ShouldResolveAllAliasesInOneBatch()
    {
        TripPlan trip = CreateTripWithMember();
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        plans.Setup(item => item.GetAccessibleAsync("user-2", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        users.Setup(item => item.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new User { Id = "user-1", PublicDisplayName = "Camille" },
                new User { Id = "user-2", PublicDisplayName = "Alex" },
            });
        TripParticipantService service = new(plans.Object, preferences.Object, users.Object);

        ApplicationResult<TripParticipantListResult> result = await service.ListAsync(
            "user-2",
            trip.Id.Value,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "Camille", "Alex" }, result.Value?.Participants.Select(item => item.DisplayName));
        Assert.False(result.Value?.CanManageRoles);
        Assert.True(result.Value?.CanLeave);
        users.Verify(item => item.GetByIdsAsync(It.IsAny<IReadOnlyCollection<string>>(), CancellationToken.None), Times.Once);
    }

    [Fact]
    public async Task ChangeRoleAsync_WhenTheActorIsNotTheOwner_ShouldReturnNotFound()
    {
        TripPlan trip = CreateTripWithMember();
        TripMember owner = trip.Members.Single(item => item.UserId == "user-1");
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        plans.Setup(item => item.GetOwnedAsync("user-2", trip.Id, CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripParticipantService service = new(plans.Object, preferences.Object, users.Object);

        ApplicationResult<TripParticipantListResult> result = await service.ChangeRoleAsync(
            "user-2",
            trip.Id.Value,
            owner.Id.Value,
            TripDelegatedRole.Editor,
            trip.Version,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("trip.plan.not-found", Assert.Single(result.Errors).Code);
        plans.VerifyAll();
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(true, true)]
    public async Task TransferOwnershipAsync_WhenTheTargetCannotUseTheAccount_ShouldReject(
        bool isActivated,
        bool isBlocked)
    {
        TripPlan trip = CreateTripWithMember();
        TripMember target = trip.Members.Single(item => item.UserId == "user-2");
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        plans.Setup(item => item.GetOwnedAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        users.Setup(item => item.GetByIdAsync("user-2", CancellationToken.None))
            .ReturnsAsync(new User
            {
                Id = "user-2",
                IsActivated = isActivated,
                IsBlocked = isBlocked,
            });
        TripParticipantService service = new(plans.Object, preferences.Object, users.Object);

        ApplicationResult<TripParticipantListResult> result = await service.TransferOwnershipAsync(
            "user-1",
            trip.Id.Value,
            target.Id.Value,
            TripDelegatedRole.Participant,
            trip.Version,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(TripPlanErrorCodes.InvalidState, Assert.Single(result.Errors).Code);
        plans.Verify(item => item.TransferOwnershipAsync(
            It.IsAny<string>(),
            It.IsAny<TripPlan>(),
            It.IsAny<long>(),
            It.IsAny<TripActivityWrite?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        users.VerifyAll();
    }

    [Fact]
    public async Task TransferOwnershipAsync_WhenTheTargetAccountWasDeleted_ShouldReject()
    {
        TripPlan trip = CreateTripWithMember();
        TripMember target = trip.Members.Single(item => item.UserId == "user-2");
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        plans.Setup(item => item.GetOwnedAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        users.Setup(item => item.GetByIdAsync("user-2", CancellationToken.None))
            .ReturnsAsync((User?)null);
        TripParticipantService service = new(plans.Object, preferences.Object, users.Object);

        ApplicationResult<TripParticipantListResult> result = await service.TransferOwnershipAsync(
            "user-1",
            trip.Id.Value,
            target.Id.Value,
            TripDelegatedRole.Participant,
            trip.Version,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        plans.Verify(item => item.TransferOwnershipAsync(
            It.IsAny<string>(),
            It.IsAny<TripPlan>(),
            It.IsAny<long>(),
            It.IsAny<TripActivityWrite?>(),
            It.IsAny<CancellationToken>()), Times.Never);
        users.VerifyAll();
    }

    [Fact]
    public async Task TransferOwnershipAsync_ShouldAuditTheActorAsOwnerBeforeTheTransfer()
    {
        TripPlan trip = CreateTripWithMember();
        long expectedVersion = trip.Version;
        TripMember target = trip.Members.Single(item => item.UserId == "user-2");
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        Mock<ITripAuditWriter> audit = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        TripActivityWrite? persistedMarker = null;
        TripActivityWrite? publishedActivity = null;
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        plans.Setup(item => item.GetOwnedAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        plans.Setup(item => item.TransferOwnershipAsync(
                "user-1",
                trip,
                expectedVersion,
                It.IsAny<TripActivityWrite?>(),
                CancellationToken.None))
            .Callback<string, TripPlan, long, TripActivityWrite?, CancellationToken>(
                (_, _, _, activity, _) => persistedMarker = activity)
            .ReturnsAsync(() => new TripPlanWriteResult(
                TripPlanWriteOutcome.Success,
                trip.Version,
                trip));
        users.Setup(item => item.GetByIdAsync("user-2", CancellationToken.None))
            .ReturnsAsync(new User
            {
                Id = "user-2",
                IsActivated = true,
                IsBlocked = false,
            });
        users.Setup(item => item.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new[]
            {
                new User { Id = "user-1", PublicDisplayName = "Camille" },
                new User { Id = "user-2", PublicDisplayName = "Alex" },
            });
        audit.Setup(item => item.AppendAsync(It.IsAny<TripActivityWrite>(), CancellationToken.None))
            .Callback<TripActivityWrite, CancellationToken>(
                (activity, _) => publishedActivity = activity)
            .ReturnsAsync(true);
        TripActivityRecorder recorder = new(audit.Object, clock.Object);
        TripParticipantService service = new(
            plans.Object,
            preferences.Object,
            users.Object,
            clock.Object,
            recorder);

        ApplicationResult<TripParticipantListResult> result = await service.TransferOwnershipAsync(
            "user-1",
            trip.Id.Value,
            target.Id.Value,
            TripDelegatedRole.Participant,
            expectedVersion,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(TripEffectiveRole.Owner, persistedMarker?.ActorRole);
        Assert.Equal(TripEffectiveRole.Owner, publishedActivity?.ActorRole);
        audit.VerifyAll();
        plans.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task LeaveAsync_WhenDepartureSucceeds_ShouldDeleteTheMembersPreferences()
    {
        using CancellationTokenSource requestCancellation = new();
        CancellationToken requestToken = requestCancellation.Token;
        TripPlan trip = CreateTripWithMember();
        long expectedVersion = trip.Version;
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripPreferenceRepository> preferences = new(MockBehavior.Strict);
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        plans.Setup(item => item.GetAccessibleAsync("user-2", trip.Id, requestToken))
            .ReturnsAsync(trip);
        plans.Setup(item => item.ReplaceAccessibleAsync(
                "user-2",
                trip,
                expectedVersion,
                It.IsAny<TripActivityWrite?>(),
                requestToken))
            .ReturnsAsync(() =>
            {
                requestCancellation.Cancel();
                return new TripPlanWriteResult(
                    TripPlanWriteOutcome.Success,
                    trip.Version,
                    trip);
            });
        preferences.Setup(item => item.CompleteDepartureCleanupAsync(
                trip.Id,
                "user-2",
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        TripParticipantService service = new(plans.Object, preferences.Object, users.Object, clock.Object);

        ApplicationResult result = await service.LeaveAsync(
            "user-2",
            trip.Id.Value,
            expectedVersion,
            requestToken);

        Assert.True(result.IsSuccess);
        Assert.DoesNotContain(trip.Members, member => member.UserId == "user-2");
        plans.VerifyAll();
        preferences.VerifyAll();
    }

    private static TripPlan CreateTripWithMember()
    {
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage été",
            TripDateProposal.None(),
            null,
            NowUtc.AddHours(-1));
        trip.PrepareAdmission(
            TripInvitationId.Parse("invitation-1"),
            "operation-1",
            "user-2",
            1,
            NowUtc.AddMinutes(2),
            NowUtc.AddMinutes(-3));
        trip.ArmAdmission("operation-1", 1);
        trip.ApplyAdmission("operation-1", 1, TripDelegatedRole.Participant, NowUtc.AddMinutes(-2));
        trip.EstablishAdmission("operation-1", 1, NowUtc.AddMinutes(-1));
        return trip;
    }
}
