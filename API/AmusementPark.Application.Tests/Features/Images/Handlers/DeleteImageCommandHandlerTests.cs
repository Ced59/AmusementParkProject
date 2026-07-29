using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.AttractionManufacturers.Ports;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Handlers;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Search.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Images.Handlers;

public sealed class DeleteImageCommandHandlerTests
{
    [Fact]
    public async Task HandleAsync_WhenImageIsReferencedByPublishedComment_ShouldRejectGenericDeletion()
    {
        Image image = new Image
        {
            Id = "image-1",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(image);
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.IsImageReferencedAsync("image-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        DeleteImageCommandHandler handler = new DeleteImageCommandHandler(
            repository.Object,
            Mock.Of<IImageBinaryStorage>(),
            Mock.Of<IParkRepository>(),
            Mock.Of<IAttractionManufacturerRepository>(),
            Mock.Of<ISearchProjectionWriter>(),
            Mock.Of<IUserRepository>(),
            comments.Object);

        ApplicationResult result = await handler.HandleAsync(
            new DeleteImageCommand("image-1"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "image.comment.referenced");
        repository.VerifyAll();
        comments.VerifyAll();
    }
}
