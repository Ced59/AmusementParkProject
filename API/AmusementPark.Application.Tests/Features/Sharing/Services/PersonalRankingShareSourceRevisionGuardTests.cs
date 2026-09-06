using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PersonalRankingShareSourceRevisionGuardTests
{
    [Fact]
    public async Task BeginIdentityMutationAsync_WhenPublicIdentityIsUnchanged_ShouldNotReserveLease()
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        PersonalRankingShareSourceRevisionGuard guard = CreateGuard(revisions.Object);
        PersonalRankingShareIdentityState identity = new PersonalRankingShareIdentityState(
            "Camille",
            "/avatars/current.webp",
            true,
            false);

        ShareSourceMutationLease? mutationLease = await guard.BeginIdentityMutationAsync(
            "owner-1",
            identity,
            identity,
            CancellationToken.None);

        Assert.Null(mutationLease);
        revisions.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BeginIdentityMutationAsync_WhenAvatarChanges_ShouldReserveOwnerLease()
    {
        ShareSourceMutationLease expectedLease = new ShareSourceMutationLease(
            "personal-ranking:owner-1",
            13.ToString("x32"));
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginMutationAsync(
                "personal-ranking:owner-1",
                CancellationToken.None))
            .ReturnsAsync(expectedLease);
        PersonalRankingShareSourceRevisionGuard guard = CreateGuard(revisions.Object);

        ShareSourceMutationLease? mutationLease = await guard.BeginIdentityMutationAsync(
            "owner-1",
            new PersonalRankingShareIdentityState("Camille", "/avatars/old.webp", true, false),
            new PersonalRankingShareIdentityState("Camille", "/avatars/new.webp", true, false),
            CancellationToken.None);

        Assert.Same(expectedLease, mutationLease);
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteMutationAsync_WhenSettlementFails_ShouldPreserveCommittedCallerFlow()
    {
        ShareSourceMutationLease mutationLease = new ShareSourceMutationLease(
            "personal-ranking:owner-1",
            14.ToString("x32"));
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("Mongo unavailable"));
        PersonalRankingShareSourceRevisionGuard guard = CreateGuard(revisions.Object);

        await guard.CompleteMutationAsync(
            mutationLease,
            sourceChanged: true,
            CancellationToken.None);

        revisions.VerifyAll();
    }

    private static PersonalRankingShareSourceRevisionGuard CreateGuard(
        IShareSourceRevisionRepository revisions)
    {
        return new PersonalRankingShareSourceRevisionGuard(
            revisions,
            NullLogger<PersonalRankingShareSourceRevisionGuard>.Instance);
    }
}
