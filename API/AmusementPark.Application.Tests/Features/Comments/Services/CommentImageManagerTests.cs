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
    public async Task PublishForCommentAsync_WhenDraftBelongsToActor_ShouldReserveItPrivately()
    {
        Image draft = CreateDraft("author-1");
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { ImageId })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "author-1",
                PendingCommentId = "comment-1",
                IsPublished = false,
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
    public async Task RequestRemovedCleanupAsync_ShouldPersistCleanupBeforeDeletion()
    {
        const string secondImageId = "11111111111111111111111111111111";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.RequestCommentImagesCleanupAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { ImageId, secondImageId })),
                "comment-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        await manager.RequestRemovedCleanupAsync(
            "comment-1",
            new[] { ImageId, secondImageId },
            CancellationToken.None);

        repository.VerifyAll();
    }

    [Fact]
    public async Task PublishForCommentAsync_WhenSecondReservationFails_ShouldLeaveFirstForReconciliation()
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
        Image firstReserved = new Image
        {
            Id = ImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "author-1",
            PendingCommentId = "comment-1",
            IsPublished = false,
        };
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository
            .Setup(value => value.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.Count == 2),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { firstDraft, secondDraft });
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "author-1",
                "comment-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(firstReserved);
        repository
            .Setup(value => value.ReserveCommentDraftAsync(
                secondImageId,
                "author-1",
                "comment-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Image?)null);
        CommentImageManager manager = new CommentImageManager(
            repository.Object,
            Mock.Of<IImageBinaryStorage>());

        ApplicationResult<IReadOnlyCollection<string>> result = await manager.PublishForCommentAsync(
            "author-1",
            "comment-1",
            new[] { ImageId, secondImageId },
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        repository.VerifyAll();
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
            .ReturnsAsync(true);
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

    [Fact]
    public async Task DeleteOwnedDraftAsync_WhenDraftIsReserved_ShouldRejectWithoutSchedulingDeletion()
    {
        Image draft = CreateDraft("author-1");
        draft.PendingCommentId = "comment-1";
        Mock<IImageRepository> repository = new Mock<IImageRepository>(MockBehavior.Strict);
        repository.Setup(value => value.GetByIdAsync(ImageId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
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
