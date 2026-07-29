using AmusementPark.Application.Abstractions;
using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Handlers;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Commands;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Images.Results;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Handlers;

public sealed class CommentImageCommandHandlersTests
{
    [Fact]
    public async Task UploadAsync_WhenActorIsModerator_ShouldCreatePrivateOwnedDraft()
    {
        User moderator = CreateActor(Role.Moderator);
        Mock<IUserRepository> users = CreateUserRepository(moderator);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images
            .Setup(value => value.GetByOwnerAsync(
                ImageOwnerType.CommentDraft,
                moderator.Id,
                ImageCategory.Comment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Array.Empty<Image>());
        Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>> uploader =
            new Mock<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>(MockBehavior.Strict);
        uploader
            .Setup(value => value.HandleAsync(
                It.Is<UploadImageCommand>(command =>
                    command.Request.Category == ImageCategory.Comment
                    && command.Request.OwnerType == ImageOwnerType.CommentDraft
                    && command.Request.OwnerId == moderator.Id
                    && !command.Request.IsPublished),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(ApplicationResult<UploadedImageResult>.Success(new UploadedImageResult
            {
                Image = new Image { Id = "image-1" },
            }));
        UploadCommentImageCommandHandler handler = new UploadCommentImageCommandHandler(
            users.Object,
            images.Object,
            uploader.Object);

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadCommentImageCommand(moderator.Id, CreateFile("image/png", 100)));

        Assert.True(result.IsSuccess);
        images.VerifyAll();
        users.VerifyAll();
        uploader.VerifyAll();
    }

    [Theory]
    [InlineData("text/html", 100)]
    [InlineData("image/png", 0)]
    [InlineData("image/png", 10485761)]
    public async Task UploadAsync_WhenFileIsInvalid_ShouldRejectBeforePipeline(string contentType, long length)
    {
        User administrator = CreateActor(Role.Admin);
        Mock<IUserRepository> users = CreateUserRepository(administrator);
        UploadCommentImageCommandHandler handler = new UploadCommentImageCommandHandler(
            users.Object,
            Mock.Of<IImageRepository>(),
            Mock.Of<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>());

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadCommentImageCommand(administrator.Id, CreateFile(contentType, length)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.invalid");
        users.VerifyAll();
    }

    [Fact]
    public async Task UploadAsync_WhenActorIsRegularUser_ShouldRejectBeforeImageLookup()
    {
        User user = CreateActor(Role.User);
        Mock<IUserRepository> users = CreateUserRepository(user);
        UploadCommentImageCommandHandler handler = new UploadCommentImageCommandHandler(
            users.Object,
            Mock.Of<IImageRepository>(),
            Mock.Of<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>());

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadCommentImageCommand(user.Id, CreateFile("image/png", 100)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.author.forbidden");
        users.VerifyAll();
    }

    [Fact]
    public async Task DeleteDraftAsync_WhenActorIsRegularUser_ShouldRejectBeforeImageLookup()
    {
        User user = CreateActor(Role.User);
        Mock<IUserRepository> users = CreateUserRepository(user);
        DeleteCommentDraftImageCommandHandler handler = new DeleteCommentDraftImageCommandHandler(
            users.Object,
            new CommentImageManager(
                Mock.Of<IImageRepository>(),
                Mock.Of<IImageBinaryStorage>()));

        ApplicationResult result = await handler.HandleAsync(
            new DeleteCommentDraftImageCommand(user.Id, "image-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.author.forbidden");
        users.VerifyAll();
    }

    [Fact]
    public async Task UploadAsync_WhenActorReachedDraftLimit_ShouldRejectBeforePipeline()
    {
        User administrator = CreateActor(Role.Admin);
        Mock<IUserRepository> users = CreateUserRepository(administrator);
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByOwnerAsync(
                ImageOwnerType.CommentDraft,
                administrator.Id,
                ImageCategory.Comment,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(Enumerable.Range(0, 24)
                .Select(static index => new Image { Id = index.ToString() })
                .ToList());
        UploadCommentImageCommandHandler handler = new UploadCommentImageCommandHandler(
            users.Object,
            images.Object,
            Mock.Of<ICommandHandler<UploadImageCommand, ApplicationResult<UploadedImageResult>>>());

        ApplicationResult<UploadedImageResult> result = await handler.HandleAsync(
            new UploadCommentImageCommand(administrator.Id, CreateFile("image/png", 100)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.images.too-many");
        users.VerifyAll();
        images.VerifyAll();
    }

    private static FilePayload CreateFile(string contentType, long length)
    {
        return new FilePayload
        {
            FileName = "image.png",
            ContentType = contentType,
            Length = length,
            Content = new MemoryStream(new byte[] { 1 }),
        };
    }

    private static User CreateActor(Role role)
    {
        return new User
        {
            Id = "actor-1",
            IsActivated = true,
            Roles = new List<Role> { role },
        };
    }

    private static Mock<IUserRepository> CreateUserRepository(User user)
    {
        Mock<IUserRepository> repository = new Mock<IUserRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdAsync(user.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return repository;
    }
}
