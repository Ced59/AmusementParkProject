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
            comments.Object);

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
