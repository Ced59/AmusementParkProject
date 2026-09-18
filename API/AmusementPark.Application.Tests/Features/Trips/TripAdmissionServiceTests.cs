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

public sealed class TripAdmissionServiceTests
{
    private static readonly DateTime NowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task AcceptAsync_ShouldEstablishTheMemberThroughTheRepairableSaga()
    {
        TripInvitation invitation = CreateInvitation(TripInvitationStatus.Active);
        TripMemberAdmissionFence fence = TripMemberAdmissionFence.Prepare(
            invitation.Id,
            "operation-hash",
            "user-2",
            1,
            NowUtc.AddMinutes(2));
        Mock<ITripAdmissionRepository> repository = new(MockBehavior.Strict);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripInvitationSecurity> security = CreateSecurity();
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        Mock<TimeProvider> clock = CreateClock();
        MockSequence sequence = new();
        repository.InSequence(sequence)
            .Setup(item => item.GetInvitationByTokenHashAsync("token-hash", CancellationToken.None))
            .ReturnsAsync(invitation);
        users.InSequence(sequence)
            .Setup(item => item.GetByIdAsync("user-2", CancellationToken.None))
            .ReturnsAsync(new User { Id = "user-2", IsActivated = true, Email = "guest@example.com" });
        repository.InSequence(sequence)
            .Setup(item => item.PrepareFenceAsync(
                invitation.TripPlanId,
                invitation.Id,
                "user-2",
                "operation-hash",
                CancellationToken.None))
            .ReturnsAsync(new TripAdmissionFenceWriteResult(TripAdmissionWriteOutcome.Success, fence));
        repository.InSequence(sequence)
            .Setup(item => item.ReserveInvitationAsync(
                invitation,
                "user-2",
                "operation-hash",
                "operation-hash",
                fence,
                CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.Success);
        repository.InSequence(sequence)
            .Setup(item => item.ArmFenceAsync(invitation.TripPlanId, fence, CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.Success);
        repository.InSequence(sequence)
            .Setup(item => item.ApplyProvisionalMemberAsync(
                invitation.TripPlanId,
                fence,
                invitation.ProposedRole,
                CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.Success);
        repository.InSequence(sequence)
            .Setup(item => item.MarkInvitationAcceptedAsync(invitation.Id, fence, CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.Success);
        repository.InSequence(sequence)
            .Setup(item => item.EstablishMemberAsync(invitation.TripPlanId, fence, CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.Success);
        TripAdmissionService service = new(
            repository.Object,
            plans.Object,
            security.Object,
            users.Object,
            clock.Object);

        ApplicationResult<TripInvitationDecisionResult> result = await service.AcceptAsync(
            "user-2",
            "opaque-token",
            "operation-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.False(result.Value?.WasReplayed);
        repository.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task AcceptAsync_WhenTheSameAcceptedOperationIsRetried_ShouldReturnTheEstablishedTrip()
    {
        TripInvitation invitation = CreateInvitation(TripInvitationStatus.Accepted);
        TripPlan trip = CreateTripWithMember();
        Mock<ITripAdmissionRepository> repository = new(MockBehavior.Strict);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripInvitationSecurity> security = CreateSecurity();
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        repository.Setup(item => item.GetInvitationByTokenHashAsync("token-hash", CancellationToken.None))
            .ReturnsAsync(invitation);
        plans.Setup(item => item.GetAccessibleAsync("user-2", invitation.TripPlanId, CancellationToken.None))
            .ReturnsAsync(trip);
        TripAdmissionService service = new(
            repository.Object,
            plans.Object,
            security.Object,
            users.Object,
            CreateClock().Object);

        ApplicationResult<TripInvitationDecisionResult> result = await service.AcceptAsync(
            "user-2",
            "opaque-token",
            "operation-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        repository.VerifyAll();
        plans.VerifyAll();
    }

    [Fact]
    public async Task ReconcileAsync_ShouldKeepAnAlreadyEstablishedAcceptanceAfterItsLeaseExpires()
    {
        TripInvitation invitation = CreateInvitation(TripInvitationStatus.Accepted);
        TripPlan trip = CreateTripWithMember();
        Mock<ITripAdmissionRepository> repository = new(MockBehavior.Strict);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<TimeProvider> expiredClock = new(MockBehavior.Strict);
        expiredClock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(NowUtc.AddMinutes(3)));
        plans.Setup(item => item.GetAccessibleAsync("user-2", invitation.TripPlanId, CancellationToken.None))
            .ReturnsAsync(trip);
        TripAdmissionService service = new(
            repository.Object,
            plans.Object,
            Mock.Of<ITripInvitationSecurity>(),
            Mock.Of<IUserRepository>(),
            expiredClock.Object);

        bool reconciled = await service.ReconcileAsync(invitation, CancellationToken.None);

        Assert.True(reconciled);
        plans.VerifyAll();
        repository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReconcileAsync_WhenAnotherNodeEstablishesTheMemberDuringCompensation_ShouldNotRevokeInvitation()
    {
        TripInvitation invitation = CreateInvitation(TripInvitationStatus.Accepted);
        Mock<ITripAdmissionRepository> repository = new(MockBehavior.Strict);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<TimeProvider> expiredClock = new(MockBehavior.Strict);
        expiredClock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(NowUtc.AddMinutes(3)));
        plans.Setup(item => item.GetAccessibleAsync("user-2", invitation.TripPlanId, CancellationToken.None))
            .ReturnsAsync((TripPlan?)null);
        repository.Setup(item => item.CancelExpiredFenceAsync(
                invitation.TripPlanId,
                It.IsAny<TripMemberAdmissionFence>(),
                CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.AlreadyCompleted);
        TripAdmissionService service = new(
            repository.Object,
            plans.Object,
            Mock.Of<ITripInvitationSecurity>(),
            Mock.Of<IUserRepository>(),
            expiredClock.Object);

        bool reconciled = await service.ReconcileAsync(invitation, CancellationToken.None);

        Assert.True(reconciled);
        repository.VerifyAll();
        repository.Verify(item => item.CancelInvitationAcceptanceAsync(
            It.IsAny<TripInvitationId>(),
            It.IsAny<TripMemberAdmissionFence>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeclineAsync_WhenTheSameOperationIsRetried_ShouldReturnAReplay()
    {
        TripInvitation invitation = CreateInvitation(TripInvitationStatus.Declined);
        Mock<ITripAdmissionRepository> repository = new(MockBehavior.Strict);
        Mock<ITripPlanRepository> plans = new(MockBehavior.Strict);
        Mock<ITripInvitationSecurity> security = CreateSecurity();
        Mock<IUserRepository> users = new(MockBehavior.Strict);
        repository.Setup(item => item.GetInvitationByTokenHashAsync("token-hash", CancellationToken.None))
            .ReturnsAsync(invitation);
        users.Setup(item => item.GetByIdAsync("user-2", CancellationToken.None))
            .ReturnsAsync(new User { Id = "user-2", IsActivated = true, Email = "guest@example.com" });
        repository.Setup(item => item.DeclineInvitationAsync(
                invitation,
                "user-2",
                "operation-hash",
                CancellationToken.None))
            .ReturnsAsync(TripAdmissionWriteOutcome.AlreadyCompleted);
        TripAdmissionService service = new(
            repository.Object,
            plans.Object,
            security.Object,
            users.Object,
            CreateClock().Object);

        ApplicationResult<TripInvitationDecisionResult> result = await service.DeclineAsync(
            "user-2",
            "opaque-token",
            "operation-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(result.Value?.WasReplayed);
        repository.VerifyAll();
        users.VerifyAll();
    }

    private static Mock<ITripInvitationSecurity> CreateSecurity()
    {
        Mock<ITripInvitationSecurity> security = new(MockBehavior.Strict);
        string tokenHash = "token-hash";
        security.Setup(item => item.TryHashPublicToken("opaque-token", out tokenHash)).Returns(true);
        security.Setup(item => item.HashOperationKey("user-2", "operation-1")).Returns("operation-hash");
        return security;
    }

    private static Mock<TimeProvider> CreateClock()
    {
        Mock<TimeProvider> clock = new(MockBehavior.Strict);
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(NowUtc));
        return clock;
    }

    private static TripInvitation CreateInvitation(TripInvitationStatus status)
    {
        return TripInvitation.Restore(
            TripInvitationId.Parse("invitation-1"),
            TripPlanId.Parse("trip-1"),
            "Voyage été",
            "token-hash",
            "hint42",
            TripDelegatedRole.Participant,
            TripMemberId.New(),
            "Camille",
            null,
            null,
            status,
            TripInvitationPreviewPolicy.ApproximatePeriod,
            TripInvitationPeriodPreview.Unspecified(),
            TripInvitationMemberCountBand.One,
            NowUtc.AddDays(1),
            null,
            status == TripInvitationStatus.Accepted ? NowUtc : null,
            status == TripInvitationStatus.Declined ? NowUtc : null,
            status == TripInvitationStatus.Accepted ? 1 : 0,
            status == TripInvitationStatus.Accepted ? "user-2" : null,
            status == TripInvitationStatus.Accepted ? "operation-hash" : null,
            status == TripInvitationStatus.Accepted ? 1 : null,
            status == TripInvitationStatus.Accepted ? NowUtc.AddMinutes(2) : null,
            NowUtc.AddHours(-1),
            NowUtc,
            status is TripInvitationStatus.Accepted or TripInvitationStatus.Declined ? 2 : 1);
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
            "operation-hash",
            "user-2",
            1,
            NowUtc.AddMinutes(2),
            NowUtc.AddMinutes(-3));
        trip.ArmAdmission("operation-hash", 1);
        trip.ApplyAdmission("operation-hash", 1, TripDelegatedRole.Participant, NowUtc.AddMinutes(-2));
        trip.EstablishAdmission("operation-hash", 1, NowUtc.AddMinutes(-1));
        return trip;
    }
}
