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
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Users;
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
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object));

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
        Assert.Equal("Alice Martin", result.Value.AuthorDisplayName);
        Assert.Equal("<p>Texte</p>", Assert.Single(result.Value.Bodies).Value);
        commentRepository.Verify(repository => repository.CreateAsync(
            It.Is<Comment>(comment =>
                comment.TargetType == CommentTargetType.Park
                && comment.TargetId == "park-1"
                && comment.ParkId == "park-1"
                && comment.AuthorUserId == "admin-1"
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
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object));

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
        GetCommentThreadQueryHandler handler = new GetCommentThreadQueryHandler(
            commentRepository.Object,
            new CommentTargetResolver(parkRepository.Object, parkItemRepository.Object));

        ApplicationResult<CommentThreadResult> result = await handler.HandleAsync(
            new GetCommentThreadQuery(CommentTargetType.Park, "park-1", false));

        Assert.True(result.IsSuccess);
        Assert.Equal(new[] { "official", "regular" }, result.Value!.Comments.Select(static comment => comment.Id));
        commentRepository.VerifyAll();
        parkRepository.VerifyAll();
        parkItemRepository.VerifyNoOtherCalls();
    }

    private static Comment CreateComment(string id, bool isOfficial, DateTime createdAtUtc)
    {
        return new Comment
        {
            Id = id,
            TargetType = CommentTargetType.Park,
            TargetId = "park-1",
            ParkId = "park-1",
            AuthorUserId = "admin-1",
            AuthorDisplayName = "Alice",
            AuthorRole = Role.Admin,
            Bodies = new List<LocalizedText> { new LocalizedText("fr", "<p>Texte</p>") },
            IsOfficial = isOfficial,
            ModerationStatus = CommentModerationStatus.Published,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
        };
    }
}
