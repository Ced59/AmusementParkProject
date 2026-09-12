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
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class DeleteImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenCurrentAvatarIsDeleted_ShouldFenceBeforeDeletionAndClearPublicAvatar()
    {
        Image avatar = new Image
        {
            Id = "avatar-1",
            Category = ImageCategory.Avatar,
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            IsCurrent = true,
            IsPublished = true,
        };
        User user = new User
        {
            Id = "owner-1",
            Email = "owner@example.com",
            AvatarUrl = "/images/avatar-1",
        };
        ShareSourceMutationLease lease = ShareSourceMutationLease.Create(
            "personal-ranking:owner-1");
        bool leaseStarted = false;

        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("avatar-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(avatar);
        images.Setup(value => value.DeleteIfUnchangedAsync(
                "avatar-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.User
                    && guard.OwnerId == "owner-1"
                    && guard.Category == ImageCategory.Avatar
                    && guard.IsCurrent),
                It.IsAny<CancellationToken>()))
            .Callback(() => Assert.True(leaseStarted))
            .ReturnsAsync(true);
        images.Setup(value => value.GetByOwnerAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);

        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync("avatar-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        users.Setup(value => value.UpdateAvatarUrlAsync(
                "owner-1",
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        Mock<IPersonalRankingShareSourceRevisionGuard> revisions =
            new Mock<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict);
        revisions.Setup(value => value.BeginAvatarMutationAsync(
                "owner-1",
                It.IsAny<CancellationToken>()))
            .Callback(() => leaseStarted = true)
            .ReturnsAsync(lease);
        revisions.Setup(value => value.CompleteMutationAsync(
                lease,
                true,
                CancellationToken.None))
            .Returns(Task.CompletedTask);

        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            images.Object,
            Mock.Of<IImageBinaryStorage>(MockBehavior.Strict),
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            users.Object,
            comments.Object,
            revisions.Object);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("avatar-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        comments.VerifyAll();
        users.VerifyAll();
        revisions.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenImageChangedAfterRead_ShouldKeepItsBinary()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.Park,
            OwnerId = "park-1",
            Path = "parks/image-1.webp",
            IsCurrent = true,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        images.Setup(value => value.DeleteIfUnchangedAsync(
                "image-1",
                It.Is<ImageMutationPrecondition>(guard =>
                    guard.OwnerType == ImageOwnerType.Park
                    && guard.OwnerId == "park-1"
                    && guard.Category == ImageCategory.Park
                    && guard.IsCurrent),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            images.Object,
            storage.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            comments.Object,
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict));

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("image-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        images.VerifyAll();
        comments.VerifyAll();
        storage.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenBinaryCleanupFailsAfterMetadataDeletion_ShouldReportCommittedDeletion()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.None,
            Path = "parks/image-1.webp",
            IsCurrent = false,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        images.Setup(value => value.DeleteIfUnchangedAsync(
                "image-1",
                It.IsAny<ImageMutationPrecondition>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(
                "parks/image-1.webp",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(
                "image-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            images.Object,
            storage.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            comments.Object,
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict));

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("image-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        storage.VerifyAll();
        comments.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenBinaryCleanupThrowsAfterMetadataDeletion_ShouldReportCommittedDeletion()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Park,
            OwnerType = ImageOwnerType.None,
            Path = "parks/image-1.webp",
            IsCurrent = false,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        images.Setup(value => value.DeleteIfUnchangedAsync(
                "image-1",
                It.IsAny<ImageMutationPrecondition>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync(
                "parks/image-1.webp",
                CancellationToken.None))
            .ThrowsAsync(new IOException("Storage unavailable."));
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync(
                "image-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            images.Object,
            storage.Object,
            Mock.Of<IParkRepository>(MockBehavior.Strict),
            Mock.Of<IAttractionManufacturerRepository>(MockBehavior.Strict),
            Mock.Of<ISearchProjectionWriter>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            comments.Object,
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>(MockBehavior.Strict));

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("image-1"),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        storage.VerifyAll();
        comments.VerifyAll();
    }

    [Theory]
    [InlineData(ImageOwnerType.Comment, false)]
    [InlineData(ImageOwnerType.CommentDraft, false)]
    [InlineData(ImageOwnerType.CommentDraft, true)]
    public async Task HandleAsync_WhenImageBelongsToManagedCommentScope_ShouldRejectBeforeReferenceLookup(
        ImageOwnerType ownerType,
        bool isReserved)
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            OwnerType = ownerType,
            OwnerId = ownerType == ImageOwnerType.Comment ? "comment-1" : "author-1",
            PendingCommentId = isReserved ? "comment-1" : null,
            IsPublished = ownerType == ImageOwnerType.Comment,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            repository.Object,
            storage.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>(),
            comments.Object,
            Mock.Of<IPersonalRankingShareSourceRevisionGuard>());

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("image-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.comment.lifecycle-managed");
        repository.VerifyAll();
        comments.VerifyNoOtherCalls();
        storage.VerifyNoOtherCalls();
    }
}
