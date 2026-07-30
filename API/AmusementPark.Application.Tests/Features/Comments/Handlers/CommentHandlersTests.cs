using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Contracts;
using AmusementPark.Application.Features.Comments.Handlers;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Queries;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.ParkItems.Ports;
using AmusementPark.Application.Features.Parks.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Handlers;

public sealed class CommentHandlersTests
{
    [Fact]
    public async Task HandleAsync_WhenAuthorIsAdmin_ShouldPublishSanitizedOfficialComment()
    {
        User author = new User
        {
            Id = "admin-1",
            FirstName = " Alice ",
            LastName = " Martin ",
            PublicDisplayName = " CoasterFan ",
            AvatarUrl = " /images/avatar-1 ",
            IsActivated = true,
            Roles = new List<Role> { Role.Admin },
        };
        Park park = new Park { Id = "park-1", Name = "Demo Park", IsVisible = true };
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.CreateAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment comment, CancellationToken _) =>
            {
                comment.Id = "comment-1";
                return comment;
            });
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml("<p>Texte<script>bad</script></p>")).Returns("<p>Texte</p>");
        sanitizer.Setup(value => value.ExtractPlainText("<p>Texte</p>")).Returns("Texte");
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        CreateCommentCommandHandler handler = new CreateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object),
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new CreateCommentCommand(
            " admin-1 ",
            new CommentWriteModel(
                CommentTargetType.Park,
                " park-1 ",
                new[] { new LocalizedTextValue("FR", "<p>Texte<script>bad</script></p>") },
                true)));

        Assert.True(result.IsSuccess);
        Assert.Equal("comment-1", result.Value!.Id);
        Assert.True(result.Value.IsOfficial);
        Assert.Equal(Role.Admin, result.Value.AuthorRole);
        Assert.Equal("CoasterFan", result.Value.AuthorDisplayName);
        Assert.Equal("/images/avatar-1", result.Value.AuthorAvatarUrl);
        Assert.Equal("<p>Texte</p>", Assert.Single(result.Value.Bodies).Value);
        commentRepository.Verify(repository => repository.CreateAsync(
            It.Is<Comment>(comment =>
                comment.TargetType == CommentTargetType.Park
                && comment.TargetId == "park-1"
                && comment.ParkId == "park-1"
                && comment.AuthorUserId == "admin-1"
                && comment.AuthorDisplayName == "CoasterFan"
                && comment.AuthorAvatarUrl == "/images/avatar-1"
                && comment.ModerationStatus == CommentModerationStatus.Published
                && comment.IsOfficial),
            It.IsAny<CancellationToken>()), Times.Once);
        commentRepository.VerifyAll();
        sanitizer.VerifyAll();
        userRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task HandleAsync_WhenAuthorIsRegularUser_ShouldRejectBeforeResolvingTarget()
    {
        User author = new User
        {
            Id = "user-1",
            IsActivated = true,
            Roles = new List<Role> { Role.User },
        };
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("user-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        CreateCommentCommandHandler handler = new CreateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object),
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new CreateCommentCommand(
            "user-1",
            new CommentWriteModel(
                CommentTargetType.Park,
                "park-1",
                new[] { new LocalizedTextValue("fr", "<p>Texte</p>") },
                false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.author.forbidden");
        commentRepository.VerifyNoOtherCalls();
        sanitizer.VerifyNoOtherCalls();
        userRepository.VerifyAll();
        parkRepository.VerifyNoOtherCalls();
        parkItemRepository.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentExists_ShouldSanitizeContentAndPreserveOwnership()
    {
        DateTime createdAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        Comment existing = CreateComment("comment-1", false, createdAtUtc);
        string authorUserId = existing.AuthorUserId;
        string targetId = existing.TargetId;
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.UpdateAsync(
                existing,
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment comment, long _, CancellationToken _) => comment);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml("<p>Corrigé<script>bad</script></p>"))
            .Returns("<p>Corrigé</p>");
        sanitizer.Setup(value => value.ExtractPlainText("<p>Corrigé</p>")).Returns("Corrigé");
        Mock<IUserRepository> userRepository = CreateAdminUserRepository();
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "admin-1",
            " comment-1 ",
            new CommentEditModel(
                new[] { new LocalizedTextValue("FR", "<p>Corrigé<script>bad</script></p>") },
                true)));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsOfficial);
        Assert.Equal("<p>Corrigé</p>", Assert.Single(result.Value.Bodies).Value);
        Assert.Equal(authorUserId, existing.AuthorUserId);
        Assert.Equal(targetId, existing.TargetId);
        Assert.Equal(createdAtUtc, existing.CreatedAtUtc);
        Assert.True(existing.UpdatedAtUtc > createdAtUtc);
        commentRepository.VerifyAll();
        sanitizer.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenCommentDoesNotExist_ShouldReturnNotFoundWithoutSanitizing()
    {
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("missing", It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = CreateAdminUserRepository();
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "admin-1",
            "missing",
            new CommentEditModel(
                new[] { new LocalizedTextValue("fr", "<p>Texte</p>") },
                false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.not-found");
        commentRepository.VerifyAll();
        sanitizer.VerifyNoOtherCalls();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenClientRevisionIsStale_ShouldReturnConflictBeforeSanitizing()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        existing.Revision = 3;
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync(
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        Mock<ICommentContentSanitizer> sanitizer =
            new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        Mock<IUserRepository> users = CreateAdminUserRepository();
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            comments.Object,
            sanitizer.Object,
            users.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(
            new UpdateCommentCommand(
                "admin-1",
                "comment-1",
                new CommentEditModel(
                    new[] { new LocalizedTextValue("fr", "<p>Obsolète</p>") },
                    false,
                    2)));

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "comment.concurrent-modification");
        comments.VerifyAll();
        sanitizer.VerifyNoOtherCalls();
        users.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenRevisionChanged_ShouldRollbackReservationAndPreparedCleanup()
    {
        const string draftImageId = "abcdef0123456789abcdef0123456789";
        const string existingImageId = "11111111111111111111111111111111";
        string? capturedReservationToken = null;
        string html =
            $"<p>Texte<img src=\"/images/{existingImageId}\" alt=\"Existing\" " +
            "class=\"rich-text__image rich-text__image--left\">" +
            $"<img src=\"/images/{draftImageId}\" alt=\"Draft\" " +
            "class=\"rich-text__image rich-text__image--right\"></p>";
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        existing.ImageIds = new List<string> { existingImageId };
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        comments.Setup(value => value.UpdateAsync(
                existing,
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment?)null);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml(html)).Returns(html);
        sanitizer.Setup(value => value.ExtractPlainText(html)).Returns("Texte");
        sanitizer.Setup(value => value.ExtractImageIds(html))
            .Returns(new[] { existingImageId, draftImageId });
        Mock<IUserRepository> users = CreateAdminUserRepository();
        Image draft = new Image
        {
            Id = draftImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "admin-1",
            IsPublished = false,
        };
        Image reserved = new Image
        {
            Id = draftImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "admin-1",
            PendingCommentId = "comment-1",
            IsPublished = false,
        };
        Image published = new Image
        {
            Id = existingImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.Comment,
            OwnerId = "comment-1",
            IsPublished = true,
            CleanupRequestedAtUtc = new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { published, draft });
        images.Setup(value => value.TryPreparePublishedCommentImageForReuseAsync(
                existingImageId,
                "comment-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(PublishedCommentImageReusePreparation.PreparedAndCleanupCleared);
        images.Setup(value => value.ReserveCommentDraftAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                string _,
                string _,
                string reservationToken,
                DateTime _,
                CancellationToken _) =>
                capturedReservationToken = reservationToken)
            .ReturnsAsync(reserved);
        images.Setup(value => value.ReleaseCommentDraftReservationAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.Is<string>(token => token == capturedReservationToken),
                CancellationToken.None))
            .ReturnsAsync(true);
        images.Setup(value => value.RequestCommentImagesCleanupAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { existingImageId })),
                "comment-1",
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .ReturnsAsync(1);
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            comments.Object,
            sanitizer.Object,
            users.Object,
            new CommentImageManager(images.Object));

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "admin-1",
            "comment-1",
            new CommentEditModel(
                new[] { new LocalizedTextValue("fr", html) },
                false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.concurrent-modification");
        comments.VerifyAll();
        sanitizer.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenRemovedImageCleanupThrows_ShouldReleaseReservationAndRethrow()
    {
        const string draftImageId = "abcdef0123456789abcdef0123456789";
        const string removedImageId = "11111111111111111111111111111111";
        string html =
            $"<p>Texte<img src=\"/images/{draftImageId}\" alt=\"Draft\" " +
            "class=\"rich-text__image rich-text__image--full\"></p>";
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        existing.ImageIds = new List<string> { removedImageId };
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        Mock<ICommentContentSanitizer> sanitizer =
            new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml(html)).Returns(html);
        sanitizer.Setup(value => value.ExtractPlainText(html)).Returns("Texte");
        sanitizer.Setup(value => value.ExtractImageIds(html)).Returns(new[] { draftImageId });
        Mock<IUserRepository> users = CreateAdminUserRepository();
        Image draft = new Image
        {
            Id = draftImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "admin-1",
            IsPublished = false,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        images.Setup(value => value.ReserveCommentDraftAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Image
            {
                Id = draftImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "admin-1",
                PendingCommentId = "comment-1",
                IsPublished = false,
            });
        images.Setup(value => value.RequestCommentImagesCleanupAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { removedImageId })),
                "comment-1",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Cleanup failed."));
        images.Setup(value => value.ReleaseCommentDraftReservationAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            comments.Object,
            sanitizer.Object,
            users.Object,
            new CommentImageManager(images.Object));

        InvalidOperationException exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => handler.HandleAsync(new UpdateCommentCommand(
                "admin-1",
                "comment-1",
                new CommentEditModel(
                    new[] { new LocalizedTextValue("fr", html) },
                    false))));

        Assert.Equal("Cleanup failed.", exception.Message);
        comments.VerifyAll();
        sanitizer.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenPersistenceIsCancelled_ShouldReleaseReservationAndRethrowCancellation()
    {
        const string draftImageId = "abcdef0123456789abcdef0123456789";
        string html =
            $"<p>Texte<img src=\"/images/{draftImageId}\" alt=\"Draft\" " +
            "class=\"rich-text__image rich-text__image--full\"></p>";
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments.Setup(value => value.GetByIdAsync("comment-1", cancellation.Token))
            .ReturnsAsync(existing);
        comments.Setup(value => value.UpdateAsync(
                existing,
                existing.Revision,
                cancellation.Token))
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        Mock<ICommentContentSanitizer> sanitizer =
            new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml(html)).Returns(html);
        sanitizer.Setup(value => value.ExtractPlainText(html)).Returns("Texte");
        sanitizer.Setup(value => value.ExtractImageIds(html)).Returns(new[] { draftImageId });
        Mock<IUserRepository> users = CreateAdminUserRepository();
        Image draft = new Image
        {
            Id = draftImageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "admin-1",
            IsPublished = false,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                cancellation.Token))
            .ReturnsAsync(new[] { draft });
        images.Setup(value => value.ReserveCommentDraftAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                cancellation.Token))
            .ReturnsAsync(new Image
            {
                Id = draftImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "admin-1",
                PendingCommentId = "comment-1",
                IsPublished = false,
            });
        images.Setup(value => value.ReleaseCommentDraftReservationAsync(
                draftImageId,
                "admin-1",
                "comment-1",
                It.IsAny<string>(),
                CancellationToken.None))
            .ReturnsAsync(true);
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            comments.Object,
            sanitizer.Object,
            users.Object,
            new CommentImageManager(images.Object));

        OperationCanceledException exception = await Assert.ThrowsAsync<OperationCanceledException>(
            () => handler.HandleAsync(
                new UpdateCommentCommand(
                    "admin-1",
                    "comment-1",
                    new CommentEditModel(
                        new[] { new LocalizedTextValue("fr", html) },
                        false)),
                cancellation.Token));

        Assert.Equal(cancellation.Token, exception.CancellationToken);
        comments.VerifyAll();
        sanitizer.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenModeratorOwnsComment_ShouldUpdateIt()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            "moderator-1",
            Role.Moderator);
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.UpdateAsync(
                existing,
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment comment, long _, CancellationToken _) => comment);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml("<p>Corrigé</p>")).Returns("<p>Corrigé</p>");
        sanitizer.Setup(value => value.ExtractPlainText("<p>Corrigé</p>")).Returns("Corrigé");
        Mock<IUserRepository> userRepository = CreateModeratorUserRepository();
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "moderator-1",
            "comment-1",
            new CommentEditModel(
                new[] { new LocalizedTextValue("fr", "<p>Corrigé</p>") },
                false)));

        Assert.True(result.IsSuccess);
        Assert.Equal("moderator-1", result.Value!.AuthorUserId);
        commentRepository.VerifyAll();
        sanitizer.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenRegularUserOwnsComment_ShouldUpdateIt()
    {
        Comment existing = CreateComment(
            "comment-1",
            true,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            "user-1",
            Role.User);
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.UpdateAsync(
                existing,
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment comment, long _, CancellationToken _) => comment);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml("<p>Corrigé</p>")).Returns("<p>Corrigé</p>");
        sanitizer.Setup(value => value.ExtractPlainText("<p>Corrigé</p>")).Returns("Corrigé");
        Mock<IUserRepository> userRepository = CreateActiveUserRepository("user-1", Role.User);
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "user-1",
            "comment-1",
            new CommentEditModel(
                new[] { new LocalizedTextValue("fr", "<p>Corrigé</p>") },
                false)));

        Assert.True(result.IsSuccess);
        Assert.Equal("user-1", result.Value!.AuthorUserId);
        Assert.True(result.Value.IsOfficial);
        commentRepository.VerifyAll();
        sanitizer.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task UpdateAsync_WhenModeratorDoesNotOwnComment_ShouldRejectBeforeSanitizing()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = CreateModeratorUserRepository();
        UpdateCommentCommandHandler handler = new UpdateCommentCommandHandler(
            commentRepository.Object,
            sanitizer.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new UpdateCommentCommand(
            "moderator-1",
            "comment-1",
            new CommentEditModel(
                new[] { new LocalizedTextValue("fr", "<p>Interdit</p>") },
                false)));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.manager.forbidden");
        commentRepository.VerifyAll();
        sanitizer.VerifyNoOtherCalls();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenCommentExists_ShouldDeleteIt()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.DeleteAsync(
                "comment-1",
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IUserRepository> userRepository = CreateAdminUserRepository();
        DeleteCommentCommandHandler handler = new DeleteCommentCommandHandler(
            commentRepository.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult result = await handler.HandleAsync(new DeleteCommentCommand(
            "admin-1",
            " comment-1 "));

        Assert.True(result.IsSuccess);
        commentRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenModeratorOwnsComment_ShouldDeleteIt()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            "moderator-1",
            Role.Moderator);
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.DeleteAsync(
                "comment-1",
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IUserRepository> userRepository = CreateModeratorUserRepository();
        DeleteCommentCommandHandler handler = new DeleteCommentCommandHandler(
            commentRepository.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult result = await handler.HandleAsync(new DeleteCommentCommand(
            "moderator-1",
            "comment-1"));

        Assert.True(result.IsSuccess);
        commentRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenRegularUserOwnsComment_ShouldDeleteIt()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            "user-1",
            Role.User);
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        commentRepository
            .Setup(repository => repository.DeleteAsync(
                "comment-1",
                existing.Revision,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        Mock<IUserRepository> userRepository = CreateActiveUserRepository("user-1", Role.User);
        DeleteCommentCommandHandler handler = new DeleteCommentCommandHandler(
            commentRepository.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult result = await handler.HandleAsync(new DeleteCommentCommand(
            "user-1",
            "comment-1"));

        Assert.True(result.IsSuccess);
        commentRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task DeleteAsync_WhenModeratorDoesNotOwnComment_ShouldRejectWithoutDeleting()
    {
        Comment existing = CreateComment(
            "comment-1",
            false,
            new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc));
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetByIdAsync("comment-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(existing);
        Mock<IUserRepository> userRepository = CreateModeratorUserRepository();
        DeleteCommentCommandHandler handler = new DeleteCommentCommandHandler(
            commentRepository.Object,
            userRepository.Object,
            CreateCommentImageManager());

        ApplicationResult result = await handler.HandleAsync(new DeleteCommentCommand(
            "moderator-1",
            "comment-1"));

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "comment.manager.forbidden");
        commentRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenThreadContainsOfficialComment_ShouldReturnItFirst()
    {
        Park park = new Park { Id = "park-1", Name = "Demo Park", IsVisible = true };
        DateTime olderDate = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc);
        DateTime newerDate = olderDate.AddDays(1);
        Comment regular = CreateComment("regular", false, newerDate);
        Comment official = CreateComment("official", true, olderDate);
        Mock<ICommentRepository> commentRepository = new Mock<ICommentRepository>(MockBehavior.Strict);
        commentRepository
            .Setup(repository => repository.GetPublishedByTargetAsync(
                CommentTargetType.Park,
                "park-1",
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { regular, official });
        Mock<IParkRepository> parkRepository = new Mock<IParkRepository>(MockBehavior.Strict);
        parkRepository
            .Setup(repository => repository.GetByIdAsync("park-1", false, It.IsAny<CancellationToken>()))
            .ReturnsAsync(park);
        Mock<IParkItemRepository> parkItemRepository = new Mock<IParkItemRepository>(MockBehavior.Strict);
        User currentAuthor = new User
        {
            Id = "admin-1",
            PublicDisplayName = "CoasterFan",
            AvatarUrl = "/images/current-avatar",
            Roles = new List<Role> { Role.Admin },
        };
        currentAuthor.AssignPublicAccountNumber(1);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdsAsync(
                It.Is<IReadOnlyCollection<string>>(ids => ids.SequenceEqual(new[] { "admin-1" })),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { currentAuthor });
        GetCommentThreadQueryHandler handler = new GetCommentThreadQueryHandler(
            commentRepository.Object,
            userRepository.Object,
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object));

        ApplicationResult<CommentThreadResult> result = await handler.HandleAsync(
            new GetCommentThreadQuery(CommentTargetType.Park, "park-1", false));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "official", "regular" }, result.Value!.Comments.Select(static comment => comment.Id));
        Assert.All(result.Value.Comments, static comment =>
        {
            Assert.Equal("CoasterFan", comment.AuthorDisplayName);
            Assert.Equal("/images/current-avatar", comment.AuthorAvatarUrl);
            Assert.Equal(Role.Admin, comment.AuthorRole);
        });
        commentRepository.VerifyAll();
        userRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    private static Comment CreateComment(
        string id,
        bool isOfficial,
        DateTime createdAtUtc,
        string authorUserId = "admin-1",
        Role authorRole = Role.Admin)
    {
        return new Comment
        {
            Id = id,
            TargetType = CommentTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            AuthorUserId = authorUserId,
            AuthorDisplayName = "Alice",
            AuthorAvatarUrl = "/images/avatar-1",
            AuthorRole = authorRole,
            Bodies = new List<LocalizedText> { new LocalizedText("fr", "<p>Texte</p>") },
            IsOfficial = isOfficial,
            ModerationStatus = CommentModerationStatus.Published,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };
    }

    private static Mock<IUserRepository> CreateAdminUserRepository()
    {
        User administrator = new User
        {
            Id = "admin-1",
            IsActivated = true,
            Roles = new List<Role> { Role.Admin },
        };
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(administrator);
        return userRepository;
    }

    [Fact]
    public async Task HandleAsync_WhenImageIsUsedInImageOnlyTranslation_ShouldKeepLanguageAndPublishUnion()
    {
        const string imageId = "abcdef0123456789abcdef0123456789";
        string? capturedReservationToken = null;
        string imageHtml =
            $"<img src=\"/images/{imageId}\" alt=\"Park\" class=\"rich-text__image rich-text__image--full\">";
        User author = new User
        {
            Id = "admin-1",
            IsActivated = true,
            Roles = new List<Role> { Role.Admin },
        };
        Mock<ICommentRepository> comments = new Mock<ICommentRepository>(MockBehavior.Strict);
        comments
            .Setup(value => value.CreateAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Comment comment, CancellationToken _) => comment);
        Mock<ICommentContentSanitizer> sanitizer = new Mock<ICommentContentSanitizer>(MockBehavior.Strict);
        sanitizer.Setup(value => value.SanitizeRichHtml("<p>Texte</p>")).Returns("<p>Texte</p>");
        sanitizer.Setup(value => value.ExtractPlainText("<p>Texte</p>")).Returns("Texte");
        sanitizer.Setup(value => value.SanitizeRichHtml(imageHtml)).Returns(imageHtml);
        sanitizer.Setup(value => value.ExtractPlainText(imageHtml)).Returns(string.Empty);
        sanitizer.Setup(value => value.ExtractImageIds(imageHtml)).Returns(new[] { imageId });
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(author);
        Mock<IParkRepository> parks = new Mock<IParkRepository>(MockBehavior.Strict);
        parks.Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
        Mock<IParkItemRepository> items = new Mock<IParkItemRepository>(MockBehavior.Strict);
        Image draft = new Image
        {
            Id = imageId,
            Category = ImageCategory.Comment,
            OwnerType = ImageOwnerType.CommentDraft,
            OwnerId = "admin-1",
            IsPublished = false,
        };
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new[] { draft });
        images.Setup(value => value.ReserveCommentDraftAsync(
                imageId,
                "admin-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .Callback((
                string _imageId,
                string _ownerId,
                string _commentId,
                string reservationToken,
                DateTime _reconcileAfterUtc,
                CancellationToken _cancellationToken) =>
                capturedReservationToken = reservationToken)
            .ReturnsAsync(new Image
            {
                Id = imageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "admin-1",
                IsPublished = false,
            });
        images.Setup(value => value.FinalizeCommentDraftAsync(
                imageId,
                "admin-1",
                It.IsAny<string>(),
                It.Is<string>(token => token == capturedReservationToken),
                CancellationToken.None))
            .ReturnsAsync(new Image
            {
                Id = imageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                IsPublished = true,
            });
        CreateCommentCommandHandler handler = new CreateCommentCommandHandler(
            comments.Object,
            sanitizer.Object,
            users.Object,
            new CommentTargetResolver(parks.Object, items.Object),
            new CommentImageManager(images.Object));

        ApplicationResult<CommentResult> result = await handler.HandleAsync(new CreateCommentCommand(
            "admin-1",
            new CommentWriteModel(
                CommentTargetType.Park,
                "park-1",
                new[]
                {
                    new LocalizedTextValue("fr", "<p>Texte</p>"),
                    new LocalizedTextValue("en", imageHtml),
                },
                false)));

        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Bodies.Count);
        comments.Verify(value => value.CreateAsync(
            It.Is<Comment>(comment =>
                comment.ImageIds.SequenceEqual(new[] { imageId })
                && comment.Bodies.Any(body => body.LanguageCode == "en")),
            It.IsAny<CancellationToken>()), Times.Once);
        comments.VerifyAll();
        sanitizer.VerifyAll();
        users.VerifyAll();
        parks.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task HandleAsync_WhenInsertWasCommittedBeforeMongoThrows_ShouldRecoverAndFinalizeReservation()
    {
        CreateCommentWithImageScenario scenario = new CreateCommentWithImageScenario();
        Comment? committed = null;
        scenario.Comments
            .Setup(value => value.CreateAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .Callback((Comment comment, CancellationToken _) => committed = comment)
            .ThrowsAsync(new InvalidOperationException("Acknowledgement lost."));
        scenario.Comments
            .Setup(value => value.GetByIdAsync(
                It.Is<string>(commentId => commentId == scenario.CommentId),
                CancellationToken.None))
            .ReturnsAsync(() => committed);
        scenario.Images
            .Setup(value => value.FinalizeCommentDraftAsync(
                CreateCommentWithImageScenario.ImageId,
                "admin-1",
                It.Is<string>(commentId => commentId == scenario.CommentId),
                It.Is<string>(token => token == scenario.ReservationToken),
                CancellationToken.None))
            .ReturnsAsync(new Image
            {
                Id = CreateCommentWithImageScenario.ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = scenario.CommentId,
                IsPublished = true,
            });

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(scenario.Command);

        Assert.True(result.IsSuccess);
        Assert.Equal(committed!.Id, result.Value!.Id);
        scenario.VerifyCommonCalls();
        scenario.VerifyReservationWasNotReleased();
    }

    [Fact]
    public async Task HandleAsync_WhenInsertWasNotObserved_ShouldKeepReservationForReconciliationAndRethrow()
    {
        CreateCommentWithImageScenario scenario = new CreateCommentWithImageScenario();
        scenario.Comments
            .Setup(value => value.CreateAsync(It.IsAny<Comment>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Persistence failed."));
        scenario.Comments
            .Setup(value => value.GetByIdAsync(
                It.Is<string>(commentId => commentId == scenario.CommentId),
                CancellationToken.None))
            .ReturnsAsync((Comment?)null);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Handler.HandleAsync(scenario.Command));

        scenario.VerifyCommonCalls();
        scenario.VerifyReservationWasNotReleased();
        scenario.VerifyReservationWasNotFinalized();
    }

    [Fact]
    public async Task HandleAsync_WhenCommittedInsertIsCancelled_ShouldVerifyWithIndependentTokenAndFinalize()
    {
        using CancellationTokenSource cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        CreateCommentWithImageScenario scenario = new CreateCommentWithImageScenario();
        Comment? committed = null;
        scenario.Comments
            .Setup(value => value.CreateAsync(
                It.IsAny<Comment>(),
                cancellation.Token))
            .Callback((Comment comment, CancellationToken _) => committed = comment)
            .ThrowsAsync(new OperationCanceledException(cancellation.Token));
        scenario.Comments
            .Setup(value => value.GetByIdAsync(
                It.Is<string>(commentId => commentId == scenario.CommentId),
                CancellationToken.None))
            .ReturnsAsync(() => committed);
        scenario.Images
            .Setup(value => value.FinalizeCommentDraftAsync(
                CreateCommentWithImageScenario.ImageId,
                "admin-1",
                It.Is<string>(commentId => commentId == scenario.CommentId),
                It.Is<string>(token => token == scenario.ReservationToken),
                CancellationToken.None))
            .ReturnsAsync(new Image
            {
                Id = CreateCommentWithImageScenario.ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.Comment,
                OwnerId = scenario.CommentId,
                IsPublished = true,
            });

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(scenario.Command, cancellation.Token);

        Assert.True(result.IsSuccess);
        scenario.VerifyCommonCalls();
        scenario.Comments.Verify(value => value.GetByIdAsync(
            It.Is<string>(commentId => commentId == scenario.CommentId),
            CancellationToken.None), Times.Once);
        scenario.VerifyReservationWasNotReleased();
    }

    private sealed class CreateCommentWithImageScenario
    {
        public const string ImageId = "abcdef0123456789abcdef0123456789";

        public CreateCommentWithImageScenario()
        {
            string html =
                $"<p>Texte<img src=\"/images/{ImageId}\" alt=\"Park\" " +
                "class=\"rich-text__image rich-text__image--full\"></p>";
            User author = new User
            {
                Id = "admin-1",
                IsActivated = true,
                Roles = new List<Role> { Role.Admin },
            };
            this.Sanitizer
                .Setup(value => value.SanitizeRichHtml(html))
                .Returns(html);
            this.Sanitizer
                .Setup(value => value.ExtractPlainText(html))
                .Returns("Texte");
            this.Sanitizer
                .Setup(value => value.ExtractImageIds(html))
                .Returns(new[] { ImageId });
            this.Users
                .Setup(value => value.GetByIdAsync("admin-1", It.IsAny<CancellationToken>()))
                .ReturnsAsync(author);
            this.Parks
                .Setup(value => value.GetByIdAsync("park-1", true, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Park { Id = "park-1", Name = "Park" });
            Image draft = new Image
            {
                Id = ImageId,
                Category = ImageCategory.Comment,
                OwnerType = ImageOwnerType.CommentDraft,
                OwnerId = "admin-1",
                IsPublished = false,
            };
            this.Images.Setup(value => value.GetByIdsAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                It.IsAny<CancellationToken>()))
                .ReturnsAsync(new[] { draft });
            this.Images.Setup(value => value.ReserveCommentDraftAsync(
                ImageId,
                "admin-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
                .Callback((
                    string _,
                    string _,
                    string commentId,
                    string reservationToken,
                    DateTime _,
                    CancellationToken _) =>
                {
                    this.CommentId = commentId;
                    this.ReservationToken = reservationToken;
                })
                .ReturnsAsync(new Image
                {
                    Id = ImageId,
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.CommentDraft,
                    OwnerId = "admin-1",
                    IsPublished = false,
                });
            this.Handler = new CreateCommentCommandHandler(
                this.Comments.Object,
                this.Sanitizer.Object,
                this.Users.Object,
                new CommentTargetResolver(this.Parks.Object, this.Items.Object),
                new CommentImageManager(this.Images.Object));
            this.Command = new CreateCommentCommand(
                "admin-1",
                new CommentWriteModel(
                    CommentTargetType.Park,
                    "park-1",
                    new[] { new LocalizedTextValue("fr", html) },
                    false));
        }

        public Mock<ICommentRepository> Comments { get; } =
            new Mock<ICommentRepository>(MockBehavior.Strict);

        public Mock<ICommentContentSanitizer> Sanitizer { get; } =
            new Mock<ICommentContentSanitizer>(MockBehavior.Strict);

        public Mock<IUserRepository> Users { get; } =
            new Mock<IUserRepository>(MockBehavior.Strict);

        public Mock<IParkRepository> Parks { get; } =
            new Mock<IParkRepository>(MockBehavior.Strict);

        public Mock<IParkItemRepository> Items { get; } =
            new Mock<IParkItemRepository>(MockBehavior.Strict);

        public Mock<IImageRepository> Images { get; } =
            new Mock<IImageRepository>(MockBehavior.Strict);

        public CreateCommentCommandHandler Handler { get; }

        public CreateCommentCommand Command { get; }

        public string? CommentId { get; private set; }

        public string? ReservationToken { get; private set; }

        public void VerifyCommonCalls()
        {
            this.Comments.VerifyAll();
            this.Sanitizer.VerifyAll();
            this.Users.VerifyAll();
            this.Parks.VerifyAll();
            this.Items.VerifyNoOtherCalls();
            this.Images.VerifyAll();
        }

        public void VerifyReservationWasNotReleased()
        {
            this.Images.Verify(value => value.ReleaseCommentDraftReservationAsync(
                ImageId,
                "admin-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None), Times.Never);
        }

        public void VerifyReservationWasNotFinalized()
        {
            this.Images.Verify(value => value.FinalizeCommentDraftAsync(
                ImageId,
                "admin-1",
                It.IsAny<string>(),
                It.IsAny<string>(),
                CancellationToken.None), Times.Never);
        }
    }

    private static CommentImageManager CreateCommentImageManager()
    {
        return new CommentImageManager(Mock.Of<IImageRepository>());
    }

    private static Mock<IUserRepository> CreateModeratorUserRepository()
    {
        return CreateActiveUserRepository("moderator-1", Role.Moderator);
    }

    private static Mock<IUserRepository> CreateActiveUserRepository(string userId, Role role)
    {
        User user = new User
        {
            Id = userId,
            IsActivated = true,
            Roles = new List<Role> { role },
        };
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(repository => repository.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);
        return userRepository;
    }
}
