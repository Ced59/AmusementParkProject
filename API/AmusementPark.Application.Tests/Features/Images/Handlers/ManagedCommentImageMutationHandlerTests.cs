using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class ManagedCommentImageMutationHandlerTests
{
    [Fact]
    public async Task LinkAsync_WhenImageBelongsToCommentLifecycle_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository = CreateCommentImageRepository();
        LinkImageCommandHandler handler = new LinkImageCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>());

        ApplicationResult<Image> result = await handler.HandleAsync(
            new LinkImageCommand(
                "comment-image",
                ImageOwnerType.Park,
                "park-1"));

        AssertManagedLifecycleFailure(result);
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task LinkAsync_WhenTargetOwnerIsCommentDraft_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync(
                "park-image",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "park-image",
                Category = ImageCategory.Park,
                OwnerType = ImageOwnerType.Park,
                OwnerId = "park-1",
            });
        LinkImageCommandHandler handler = new LinkImageCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>());

        ApplicationResult<Image> result = await handler.HandleAsync(
            new LinkImageCommand(
                "park-image",
                ImageOwnerType.CommentDraft,
                "author-1"));

        AssertManagedLifecycleFailure(result);
        imageRepository.VerifyAll();
    }

    [Fact]
    public async Task SetCurrentAsync_WhenImageBelongsToCommentLifecycle_ShouldRejectBeforeMutation()
    {
        Mock<IImageRepository> imageRepository = CreateCommentImageRepository();
        SetCurrentImageCommandHandler handler = new SetCurrentImageCommandHandler(
            imageRepository.Object,
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>());

        ApplicationResult<Image> result = await handler.HandleAsync(
            new SetCurrentImageCommand(
                "comment-image",
                ImageOwnerType.Comment,
                "comment-1"));

        AssertManagedLifecycleFailure(result);
        imageRepository.VerifyAll();
    }

    private static Mock<IImageRepository> CreateCommentImageRepository()
    {
        Mock<IImageRepository> imageRepository =
            new Mock<IImageRepository>(MockBehavior.Strict);
        imageRepository
            .Setup(repository => repository.GetByIdAsync(
                "comment-image",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = "comment-image",
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
            });
        return imageRepository;
    }

    private static void AssertManagedLifecycleFailure(ApplicationResult<Image> result)
    {
        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "image.comment.lifecycle-managed");
    }
}
