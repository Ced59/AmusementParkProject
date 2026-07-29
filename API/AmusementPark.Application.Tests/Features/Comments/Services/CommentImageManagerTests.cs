using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Images;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Services;

public sealed class CommentImageManagerTests
{
    private const string ImageId = "abcdef0123456789abcdef0123456789";

    [Fact]
    public async Task PublishForCommentAsync_WhenDraftBelongsToActor_ShouldPublishItForComment()
    {
        Image draft = CreateDraft("author-1");
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { ImageId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        repository
            .Setup(value => value.PublishCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = "comment-1",
                IsPublished = true,
            });
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenDraftBelongsToAnotherActor_ShouldRejectIt()
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { CreateDraft("other-author") });
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenImageIsAlreadyOnSameComment_ShouldKeepIt()
    {
        Image published = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { published });
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult result = await manager.PublishForCommentAsync(
            "admin-editing-another-comment",
            "comment-1",
            new[] { ImageId },
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenMoreThanLimit_ShouldRejectBeforeReadingRepository()
    {
        CommentImageManager manager = new CommentImageManager(
            Mock.Of<IImageRepository>(),
            Mock.Of<IImageBinaryStorage>());
        string[] ids = Enumerable.Range(0, CommentImageManager.MaximumImagesPerComment + 1)
            .Select(static index => index.ToString("x32"))
            .ToArray();

        ApplicationResult result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            ids,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.images.too-many");
    }

    [Fact]
    public async Task DeleteRemovedAsync_ShouldDeleteOnlyImagesOwnedByComment()
    {
        Image owned = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            Path = "comment/image-1",
            IsPublished = true,
        };
        Image foreign = new Image
        {
            Id = "11111111111111111111111111111111",
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-2",
            Path = "comment/image-2",
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { owned, foreign });
        repository
            .Setup(value => value.DeleteCommentImageAsync(
                ImageId,
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage
            .Setup(value => value.DeleteAsync("comment/image-1", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        CommentImageManager manager = new CommentImageManager(repository.Object, storage.Object);

        await manager.DeleteRemovedAsync(
            "comment-1",
            new[] { ImageId, foreign.Id },
            CancellationToken.None);

        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenSecondPublicationFails_ShouldRollbackFirstPublication()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Image firstDraft = CreateDraft("author-1");
        Image secondDraft = new Image
        {
            Id = secondImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            IsPublished = false,
        };
        Image firstPublished = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            Path = "comment/first",
            IsPublished = true,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstDraft, secondDraft });
        repository
            .Setup(value => value.PublishCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstPublished);
        repository
            .Setup(value => value.PublishCommentDraftAsync(
                secondImageId,
                "author-1",
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { ImageId })),
                CancellationToken.None))
            .ReturnsAsync(new[] { firstPublished });
        repository
            .Setup(value => value.DeleteCommentImageAsync(ImageId, "comment-1", CancellationToken.None))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync("comment/first", CancellationToken.None))
            .Returns(Task.CompletedTask);
        CommentImageManager manager = new CommentImageManager(repository.Object, storage.Object);

        ApplicationResult<IReadOnlyCollection<string>> result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId, secondImageId },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteExpiredDraftsAsync_ShouldUseAtomicDraftDeletionAndBoundedQuery()
    {
        Image draft = CreateDraft("author-1");
        draft.Path = "comment/draft";
        DateTime cutoff = new DateTime(2026, 7, 28, 10, 0, 0, DateTimeKind.Utc);
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetExpiredCommentDraftsAsync(
                cutoff,
                50,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        repository
            .Setup(value => value.DeleteCommentDraftAsync(
                ImageId,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IImageBinaryStorage> storage = new Mock<IImageBinaryStorage>(MockBehavior.Strict);
        storage.Setup(value => value.DeleteAsync("comment/draft", It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        CommentImageManager manager = new CommentImageManager(repository.Object, storage.Object);

        int result = await manager.DeleteExpiredDraftsAsync(cutoff, 50, CancellationToken.None);

        Assert.Equal(1, result);
        repository.VerifyAll();
        storage.VerifyAll();
    }

    [Fact]
    public async Task DeleteOwnedDraftAsync_WhenDraftBelongsToAnotherActor_ShouldRejectWithoutDeletion()
    {
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(ImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateDraft("other-author"));
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult result = await manager.DeleteOwnedDraftAsync(
            "author-1",
            ImageId,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.image.forbidden");
        repository.VerifyAll();
    }

    private static Image CreateDraft(string ownerId)
    {
        return new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = ownerId,
            IsPublished = false,
        };
    }
}
