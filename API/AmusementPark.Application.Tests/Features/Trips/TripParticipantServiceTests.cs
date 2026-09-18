using AmusementPark.Application.Errors;
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
        TripParticipantService service = new(plans.Object, users.Object);

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
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        plans.Setup(item => item.GetOwnedAsync("user-2", trip.Id, CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        TripParticipantService service = new(plans.Object, users.Object);

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
