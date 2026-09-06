using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class LinkImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCurrentAvatarChangesOwner_ShouldFenceAndSynchronizeBothOwners()
    {
        Image existing = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-old",
            IsCurrent = true,
            IsPublished = true,
        };
        Image updated = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-new",
            IsCurrent = true,
            IsPublished = true,
        };
        User previousOwner = new User
        {
            Id = "owner-old",
            Email = "old@example.com",
            AvatarUrl = "/images/avatar-1",
        };
        User nextOwner = new User
        {
            Id = "owner-new",
            Email = "new@example.com",
        };
        ShareSourceMutationLease previousLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-old");
        ShareSourceMutationLease nextLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-new");
        bool previousLeaseStarted = false;
        bool nextLeaseStarted = false;

        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("avatar-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        images.Setup(value => value.SetCurrentIfUnchangedAsync(
                "avatar-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-old"
                    && guard.Category == ImageCategory.Avatar
                    && guard.IsCurrent),
                ImageOwnerType.User,
                "owner-new",
                It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                Assert.True(previousLeaseStarted);
                Assert.True(nextLeaseStarted);
            })
            .ReturnsAsync(updated);
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-old",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-new",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(updated);

        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-old", It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousOwner);
        users.Setup(value => value.GetByIdAsync("owner-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(nextOwner);
        users.Setup(value => value.UpdateAsync(
                "owner-old",
                It.Is<User>(user => user.AvatarUrl == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User user, CancellationToken _) => user);
        users.Setup(value => value.UpdateAsync(
                "owner-new",
                It.Is<User>(user => user.AvatarUrl == "/images/avatar-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((string _, User user, CancellationToken _) => user);

        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginMutationAsync(
                "owner-old",
                It.IsAny<CancellationToken>()))
            .Callback(() => previousLeaseStarted = true)
            .ReturnsAsync(previousLease);
        revisions.Setup(value => value.BeginMutationAsync(
                "owner-new",
                It.IsAny<CancellationToken>()))
            .Callback(() => nextLeaseStarted = true)
            .ReturnsAsync(nextLease);
        revisions.Setup(value => value.CompleteMutationAsync(
                previousLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        revisions.Setup(value => value.CompleteMutationAsync(
                nextLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        LinkImageCommandHandler handler = new LinkImageCommandHandler(
            images.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            users.Object,
            revisions.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(
            new LinkImageCommand(
                "avatar-1",
                ImageOwnerType.User,
                "owner-new"));

        Assert.True(result.IsSuccess);
        Assert.Null(previousOwner.AvatarUrl);
        Assert.Equal("/images/avatar-1", nextOwner.AvatarUrl);
        images.VerifyAll();
        users.VerifyAll();
        revisions.VerifyAll();
    }
}
