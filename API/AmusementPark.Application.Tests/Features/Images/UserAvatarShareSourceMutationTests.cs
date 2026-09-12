using AmusementPark.Application.Features.Images;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images;

public sealed class UserAvatarShareSourceMutationTests
{
    [Fact]
    public async Task CompleteAsync_WhenPublishedAvatarIsUnchanged_ShouldNotAdvanceRevision()
    {
        Image avatar = CreatePublishedAvatar("avatar-1");
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1");
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.SetupSequence(repository => repository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(avatar)
            .ReturnsAsync(avatar);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(guard => guard.BeginAvatarMutationAsync(
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(lease);
        revisions.Setup(guard => guard.CompleteMutationAsync(
                lease,
                false,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        UserAvatarShareSourceMutationContext context =
            await UserAvatarShareSourceMutation.BeginAsync(
                new[] { "owner-1" },
                revisions.Object,
                images.Object,
                CancellationToken.None);

        await UserAvatarShareSourceMutation.CompleteAsync(
            context,
            images.Object,
            revisions.Object);

        images.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task CompleteAsync_WhenPublishedAvatarChanges_ShouldAdvanceRevision()
    {
        Image avatar = CreatePublishedAvatar("avatar-new");
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1");
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.SetupSequence(repository => repository.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null)
            .ReturnsAsync(avatar);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(guard => guard.BeginAvatarMutationAsync(
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(lease);
        revisions.Setup(guard => guard.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        UserAvatarShareSourceMutationContext context =
            await UserAvatarShareSourceMutation.BeginAsync(
                new[] { "owner-1" },
                revisions.Object,
                images.Object,
                CancellationToken.None);

        await UserAvatarShareSourceMutation.CompleteAsync(
            context,
            images.Object,
            revisions.Object);

        images.VerifyAll();
        revisions.VerifyAll();
    }

    private static Image CreatePublishedAvatar(string imageId)
    {
        return new Image
        {
            Id = imageId,
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            IsCurrent = true,
            IsPublished = true,
        };
    }
}
