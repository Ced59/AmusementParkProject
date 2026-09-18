using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Trips.Models;
using AmusementPark.Application.Features.Trips.Ports;
using AmusementPark.Application.Features.Trips.Results;
using AmusementPark.Application.Features.Trips.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Trips;
using AmusementPark.Core.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Trips;

public sealed class TripInvitationServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldCreateAnOpaqueTargetedInvitationUnderTheTripLease()
    {
        DateTime nowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage été",
            TripDateProposal.None(),
            null,
            nowUtc);
        TripMember owner = Assert.Single(trip.Members);
        TripChildMutationLease lease = new(
            "operation-hash",
            owner.Id,
            trip.ChildMutationEpoch,
            1,
            nowUtc.AddSeconds(20));
        Mock<ITripPlanRepository> trips = new();
        Mock<ITripInvitationRepository> invitations = new();
        Mock<ITripInvitationSecurity> security = new();
        Mock<ITripChildMutationLeaseRepository> leases = new();
        Mock<IUserRepository> users = new();
        Mock<TimeProvider> clock = new();
        TripInvitation? persisted = null;
        string revealedToken = "opaque-token";

        trips.Setup(item => item.GetOwnedAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        invitations.Setup(item => item.ResolveCreationAsync(
                trip.Id,
                owner.Id,
                "operation-hash",
                CancellationToken.None))
            .ReturnsAsync((TripInvitationCreationRecord?)null);
        invitations.Setup(item => item.CreateAsync(
                It.IsAny<TripInvitation>(),
                lease,
                "operation-hash",
                "request-hash",
                "sealed-token",
                "v1",
                It.IsAny<TripActivityWrite?>(),
                CancellationToken.None))
            .ReturnsAsync((
                TripInvitation invitation,
                TripChildMutationLease _,
                string operationHash,
                string requestHash,
                string sealedToken,
                string keyVersion,
                TripActivityWrite? _,
                CancellationToken _) =>
            {
                persisted = invitation;
                return new TripInvitationCreationWriteResult(
                    TripInvitationCreationWriteOutcome.Success,
                    new TripInvitationCreationRecord(
                        invitation,
                        operationHash,
                        requestHash,
                        sealedToken,
                        keyVersion,
                        false));
            });
        security.Setup(item => item.HashOperationKey("user-1", "operation-1"))
            .Returns("operation-hash");
        security.Setup(item => item.HashCreationPayload(
                TripDelegatedRole.Editor,
                24,
                "guest@example.com",
                null))
            .Returns("request-hash");
        security.Setup(item => item.FingerprintEmail("guest@example.com"))
            .Returns(new TripInvitationEmailFingerprint("email-hmac", "v1"));
        security.Setup(item => item.CreateToken(
                It.IsAny<TripInvitationId>(),
                "user-1",
                "operation-hash",
                "request-hash"))
            .Returns(new TripInvitationTokenMaterial(
                "opaque-token",
                "token-hash",
                "hint42",
                "sealed-token",
                "v1"));
        security.Setup(item => item.TryRevealToken(
                It.IsAny<TripInvitationCreationRecord>(),
                "user-1",
                out revealedToken))
            .Returns(true);
        leases.Setup(item => item.TryAcquireOwnedAsync(
                trip.Id,
                "user-1",
                owner.Id,
                trip.Version,
                trip.ChildMutationEpoch,
                "operation-hash",
                CancellationToken.None))
            .ReturnsAsync(lease);
        leases.Setup(item => item.ReleaseAsync(trip.Id, lease, CancellationToken.None))
            .Returns(Task.CompletedTask);
        users.Setup(item => item.GetByIdAsync("user-1", CancellationToken.None))
            .ReturnsAsync(new User { PublicDisplayName = " CoasterCamille " });
        clock.Setup(item => item.GetUtcNow()).Returns(new DateTimeOffset(nowUtc));
        TripInvitationService service = new(
            trips.Object,
            invitations.Object,
            security.Object,
            new TripChildMutationExecutor(leases.Object, NullLogger<TripChildMutationExecutor>.Instance),
            users.Object,
            clock.Object);

        ApplicationResult<TripInvitationCreationResult> result = await service.CreateAsync(
            "user-1",
            trip.Id.Value,
            "operation-1",
            new TripInvitationCreateInput(
                trip.Version,
                TripDelegatedRole.Editor,
                24,
                " Guest@Example.com "),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("opaque-token", result.Value?.Token);
        Assert.Equal("CoasterCamille", result.Value?.InviterDisplayName);
        Assert.Equal("CoasterCamille", persisted?.InviterDisplayName);
        Assert.Equal(TripDelegatedRole.Editor, result.Value?.ProposedRole);
        Assert.True(result.Value?.IsTargeted);
        Assert.Equal("email-hmac", persisted?.TargetEmailHmac);
        Assert.Equal(TripInvitationMemberCountBand.One, persisted?.MemberCountBand);
        leases.VerifyAll();
    }

    [Fact]
    public async Task PreviewAsync_ShouldReturnOnlyTheBoundedPublicSnapshot()
    {
        Mock<ITripInvitationSecurity> security = new();
        Mock<ITripInvitationRepository> invitations = new();
        string tokenHash = "token-hash";
        security.Setup(item => item.TryHashPublicToken("opaque-token", out tokenHash))
            .Returns(true);
        invitations.Setup(item => item.GetPublicByTokenHashAsync(
                "token-hash",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateInvitation());
        TripInvitationService service = CreateService(invitations.Object, security.Object);

        ApplicationResult<TripInvitationPreviewResult> result = await service.PreviewAsync(
            "opaque-token",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        TripInvitationPreviewResult preview = Assert.IsType<TripInvitationPreviewResult>(result.Value);
        Assert.Equal("Voyage été", preview.TripTitle);
        Assert.Equal("Camille", preview.InviterDisplayName);
        Assert.Equal(TripDelegatedRole.Participant, preview.ProposedRole);
        Assert.Equal(TripInvitationPeriodKind.SingleMonth, preview.PeriodKind);
        Assert.Equal(TripInvitationMemberCountBand.TwoToFive, preview.MemberCountBand);
    }

    [Fact]
    public async Task ListAsync_ShouldReturnTheExactPublicAliasUsedByTheGuestPreview()
    {
        DateTime nowUtc = new(2027, 6, 1, 8, 0, 0, DateTimeKind.Utc);
        TripPlan trip = TripPlan.Create(
            TripPlanId.Parse("trip-1"),
            "user-1",
            "Voyage été",
            TripDateProposal.None(),
            null,
            nowUtc);
        Mock<ITripPlanRepository> trips = new();
        Mock<ITripInvitationRepository> invitations = new();
        Mock<IUserRepository> users = new();
        trips.Setup(item => item.GetOwnedAsync("user-1", trip.Id, CancellationToken.None))
            .ReturnsAsync(trip);
        invitations.Setup(item => item.ListActiveAsync(trip.Id, CancellationToken.None))
            .ReturnsAsync(Array.Empty<TripInvitation>());
        users.Setup(item => item.GetByIdAsync("user-1", CancellationToken.None))
            .ReturnsAsync(new User { PublicDisplayName = " CoasterCamille " });
        TripInvitationService service = CreateService(
            invitations.Object,
            Mock.Of<ITripInvitationSecurity>(),
            trips.Object,
            users.Object);

        ApplicationResult<TripInvitationListResult> result = await service.ListAsync(
            "user-1",
            trip.Id.Value,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("CoasterCamille", result.Value?.InviterDisplayName);
    }

    [Fact]
    public async Task PreviewAsync_ShouldNotQueryPersistenceForAMalformedToken()
    {
        Mock<ITripInvitationSecurity> security = new();
        Mock<ITripInvitationRepository> invitations = new();
        string tokenHash = string.Empty;
        security.Setup(item => item.TryHashPublicToken("malformed", out tokenHash))
            .Returns(false);
        TripInvitationService service = CreateService(invitations.Object, security.Object);

        ApplicationResult<TripInvitationPreviewResult> result = await service.PreviewAsync(
            "malformed",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        invitations.Verify(item => item.GetPublicByTokenHashAsync(
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()), Times.Never);
    }

    private static TripInvitationService CreateService(
        ITripInvitationRepository invitations,
        ITripInvitationSecurity security,
        ITripPlanRepository? trips = null,
        IUserRepository? users = null)
    {
        TripChildMutationExecutor executor = new(
            Mock.Of<ITripChildMutationLeaseRepository>(),
            NullLogger<TripChildMutationExecutor>.Instance);
        return new TripInvitationService(
            trips ?? Mock.Of<ITripPlanRepository>(),
            invitations,
            security,
            executor,
            users ?? Mock.Of<IUserRepository>());
    }

    private static TripInvitation CreateInvitation()
    {
        DateTime nowUtc = DateTime.UtcNow;
        return TripInvitation.Create(
            TripInvitationId.Parse("invitation-1"),
            TripPlanId.Parse("trip-1"),
            "Voyage été",
            "token-hash",
            "hint42",
            TripDelegatedRole.Participant,
            TripMemberId.Parse("member-1"),
            "Camille",
            null,
            null,
            TripInvitationPeriodPreview.FromDates(new[] { new DateOnly(2027, 7, 1) }),
            TripInvitationMemberCountBand.TwoToFive,
            nowUtc,
            nowUtc.AddDays(7));
    }
}
