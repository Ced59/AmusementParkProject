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

public sealed class SetCurrentImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenUserAvatarChanges_ShouldFenceThePublicSourceRevision()
    {
        Image avatar = new Image
        {
            Id = "avatar-new",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            IsCurrent = true,
            IsPublished = true,
        };
        User user = new User
        {
            Id = "owner-1",
            PublicDisplayName = "Camille",
            AvatarUrl = "/images/avatar-old",
            IsActivated = true,
        };
        using CancellationTokenSource callerCancellation = new CancellationTokenSource();
        using CancellationTokenSource leaseCancellation = new CancellationTokenSource();
        ShareSourceMutationLease mutationLease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1",
            leaseCancellation.Token);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        MockSequence sequence = new MockSequence();
        images.Setup(value => value.GetByIdAsync("avatar-new", It.IsAny<CancellationToken>()))
            .ReturnsAsync(avatar);
        revisions.InSequence(sequence).Setup(value => value.BeginMutationAsync(
                "owner-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(mutationLease);
        images.InSequence(sequence).Setup(value => value.SetCurrentIfUnchangedAsync(
                "avatar-new",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-1"
                    && guard.Category == ImageCategory.Avatar),
                ImageOwnerType.User,
                "owner-1",
                It.Is<CancellationToken>(token => token.CanBeCanceled),
                It.Is<CancellationToken>(token => token.CanBeCanceled)))
            .Callback(() => callerCancellation.Cancel())
            .ReturnsAsync(avatar);
        users.InSequence(sequence).Setup(value => value.GetByIdAsync(
                "owner-1",
                It.Is<CancellationToken>(token =>
                    token.CanBeCanceled && !token.IsCancellationRequested)))
            .ReturnsAsync(user);
        images.InSequence(sequence).Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.Is<CancellationToken>(token =>
                    token.CanBeCanceled && !token.IsCancellationRequested)))
            .ReturnsAsync(avatar);
        users.InSequence(sequence).Setup(value => value.UpdateAvatarUrlAsync(
                "owner-1",
                "/images/avatar-new",
                It.Is<CancellationToken>(token =>
                    token.CanBeCanceled && !token.IsCancellationRequested)))
            .ReturnsAsync(true);
        revisions.InSequence(sequence).Setup(value => value.CompleteMutationAsync(
                mutationLease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);
        SetCurrentImageCommandHandler handler = new SetCurrentImageCommandHandler(
            images.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            users.Object,
            revisions.Object);

        ApplicationResult<Image> result = await handler.HandleAsync(
            new SetCurrentImageCommand("avatar-new", ImageOwnerType.User, "owner-1"),
            callerCancellation.Token);

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        users.VerifyAll();
        revisions.VerifyAll();
    }
}
