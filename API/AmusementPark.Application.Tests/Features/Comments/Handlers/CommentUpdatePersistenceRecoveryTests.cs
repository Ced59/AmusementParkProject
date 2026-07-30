using AmusementPark.Application.Common.Contracts;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Comments.Commands;
using AmusementPark.Application.Features.Comments.Contracts;
using AmusementPark.Application.Features.Comments.Handlers;
using AmusementPark.Application.Features.Comments.Ports;
using AmusementPark.Application.Features.Comments.Results;
using AmusementPark.Application.Features.Comments.Services;
using AmusementPark.Application.Features.Images.Contracts;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Comments;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Users;
using AmusementPark.Core.Localization;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Comments.Handlers;

public sealed class CommentUpdatePersistenceRecoveryTests
{
    [Fact]
    public async Task HandleAsync_WhenReplacementWasCommittedBeforeMongoThrows_ShouldFinalizeReservation()
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario();
        Comment? committed = null;
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Acknowledgement lost."),
            (comment, expectedRevision) =>
            {
                comment.Revision = expectedRevision + 1;
                committed = comment;
            });
        scenario.SetupRecovery(() => committed);
        scenario.SetupFinalization();

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken);

        Assert.True(result.IsSuccess);
        Assert.Equal(scenario.InitialRevision + 1, result.Value!.Revision);
        Assert.Equal(scenario.UpdatedHtml, Assert.Single(result.Value.Bodies).Value);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
    }

    [Theory]
    [InlineData("absent")]
    [InlineData("revision")]
    [InlineData("content")]
    public async Task HandleAsync_WhenReplacementOutcomeIsAbsentOrInconsistent_ShouldKeepReservation(
        string outcome)
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario();
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Persistence failed."),
            static (comment, expectedRevision) =>
                comment.Revision = expectedRevision + 1);
        scenario.SetupRecovery(() => outcome switch
        {
            "revision" => scenario.CreateRecoveryCandidate(
                scenario.Existing.Revision + 1,
                true),
            "content" => scenario.CreateRecoveryCandidate(
                scenario.Existing.Revision,
                false),
            _ => null,
        });

        InvalidOperationException exception =
            await Assert.ThrowsAsync<InvalidOperationException>(
                () => scenario.Handler.HandleAsync(
                    scenario.Command,
                    scenario.OperationToken));

        Assert.Equal("Persistence failed.", exception.Message);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
        scenario.VerifyReservationWasNotFinalized();
    }

    [Fact]
    public async Task HandleAsync_WhenOutcomeIsUnknown_ShouldRestoreOnlyPreparedPublishedCleanup()
    {
        using UpdateWithImageScenario scenario =
            new UpdateWithImageScenario(false, true);
        scenario.SetupUpdateFailure(
            new InvalidOperationException("Persistence failed."),
            static (comment, expectedRevision) =>
                comment.Revision = expectedRevision + 1);
        scenario.SetupRecovery(static () => null);
        scenario.Images
            .Setup(repository => repository.RequestCommentImagesCleanupAsync(
                It.Is<IReadOnlyCollection<string>>(ids =>
                    ids.SequenceEqual(new[] { UpdateWithImageScenario.PublishedImageId })),
                UpdateWithImageScenario.CommentId,
                It.IsAny<long>(),
                It.IsAny<DateTime>(),
                CancellationToken.None))
            .ReturnsAsync(1);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken));

        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
        scenario.VerifyReservationWasNotFinalized();
    }

    [Fact]
    public async Task HandleAsync_WhenCommittedReplacementIsCancelled_ShouldRecoverWithIndependentToken()
    {
        using UpdateWithImageScenario scenario = new UpdateWithImageScenario(true);
        Comment? committed = null;
        scenario.SetupUpdateFailure(
            new OperationCanceledException(scenario.OperationToken),
            (comment, expectedRevision) =>
            {
                comment.Revision = expectedRevision + 1;
                committed = comment;
            });
        scenario.SetupRecovery(() => committed);
        scenario.SetupFinalization();

        ApplicationResult<CommentResult> result =
            await scenario.Handler.HandleAsync(
                scenario.Command,
                scenario.OperationToken);

        Assert.True(result.IsSuccess);
        scenario.Comments.Verify(repository => repository.GetByIdAsync(
            UpdateWithImageScenario.CommentId,
            CancellationToken.None), Times.Once);
        scenario.VerifyAll();
        scenario.VerifyReservationWasNotReleased();
    }

    private sealed class UpdateWithImageScenario : IDisposable
    {
        public const string CommentId = "comment-1";
        public const string DraftImageId = "abcdef0123456789abcdef0123456789";
        public const string PublishedImageId = "11111111111111111111111111111111";
        private readonly CancellationTokenSource operationCancellation =
            new CancellationTokenSource();

        public UpdateWithImageScenario(
            bool cancelOperation = false,
            bool includePreparedPublishedImage = false)
        {
            if (cancelOperation)
            {
                this.operationCancellation.Cancel();
            }

            this.UpdatedHtml = includePreparedPublishedImage
                ? $"<p>Updated<img src=\"/images/{PublishedImageId}\" alt=\"Published\" " +
                    "class=\"rich-text__image rich-text__image--left\">" +
                    $"<img src=\"/images/{DraftImageId}\" alt=\"Draft\" " +
                    "class=\"rich-text__image rich-text__image--right\"></p>"
                : $"<p>Updated<img src=\"/images/{DraftImageId}\" alt=\"Draft\" " +
                    "class=\"rich-text__image rich-text__image--full\"></p>";
            List<string> desiredImageIds = includePreparedPublishedImage
                ? new List<string> { PublishedImageId, DraftImageId }
                : new List<string> { DraftImageId };
            this.Existing = new Comment
            {
                Id = CommentId,
                TargetType = CommentTargetType.Park,
                TargetId = "park-1",
                ParkId = "park-1",
                AuthorUserId = "admin-1",
                AuthorDisplayName = "Admin01",
                AuthorRole = Role.Admin,
                Bodies = new List<LocalizedText>
                {
                    new LocalizedText("fr", "<p>Old</p>"),
                },
                ImageIds = new List<string>(),
                Revision = this.InitialRevision,
                ModerationStatus = CommentModerationStatus.Published,
                CreatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
                UpdatedAtUtc = new DateTime(2026, 7, 1, 10, 0, 0, DateTimeKind.Utc),
            };
            User actor = new User
            {
                Id = "admin-1",
                PublicDisplayName = "Admin01",
                IsActivated = true,
                Roles = new List<Role> { Role.Admin },
            };
            this.Users
                .Setup(repository => repository.GetByIdAsync(
                    "admin-1",
                    this.OperationToken))
                .ReturnsAsync(actor);
            this.Comments
                .Setup(repository => repository.GetByIdAsync(
                    CommentId,
                    this.OperationToken))
                .ReturnsAsync(this.Existing);
            this.Sanitizer
                .Setup(value => value.SanitizeRichHtml(this.UpdatedHtml))
                .Returns(this.UpdatedHtml);
            this.Sanitizer
                .Setup(value => value.ExtractPlainText(this.UpdatedHtml))
                .Returns("Updated");
            this.Sanitizer
                .Setup(value => value.ExtractImageIds(this.UpdatedHtml))
                .Returns(desiredImageIds);
            List<Image> desiredImages = new List<Image>();
            if (includePreparedPublishedImage)
            {
                desiredImages.Add(new Image
                {
                    Id = PublishedImageId,
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.Comment,
                    OwnerId = CommentId,
                    IsPublished = true,
                    CleanupRequestedAtUtc =
                        new DateTime(2026, 7, 1, 11, 0, 0, DateTimeKind.Utc),
                });
                this.Images
                    .Setup(repository => repository.TryPreparePublishedCommentImageForReuseAsync(
                        PublishedImageId,
                        CommentId,
                        this.OperationToken))
                    .ReturnsAsync(
                        PublishedCommentImageReusePreparation.PreparedAndCleanupCleared);
            }

            desiredImages.Add(
                new Image
                {
                    Id = DraftImageId,
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.CommentDraft,
                    OwnerId = "admin-1",
                    IsPublished = false,
                });
            this.Images
                .Setup(repository => repository.GetByIdsAsync(
                    It.Is<IReadOnlyCollection<string>>(
                        ids => ids.SequenceEqual(desiredImageIds)),
                    this.OperationToken))
                .ReturnsAsync(desiredImages);
            this.Images
                .Setup(repository => repository.ReserveCommentDraftAsync(
                    DraftImageId,
                    "admin-1",
                    CommentId,
                    It.IsAny<string>(),
                    It.IsAny<long>(),
                    It.IsAny<DateTime>(),
                    this.OperationToken))
                .Callback((
                    string _,
                    string _,
                    string _,
                    string reservationToken,
                    long _,
                    DateTime _,
                    CancellationToken _) =>
                    this.ReservationToken = reservationToken)
                .ReturnsAsync(new Image
                {
                    Id = DraftImageId,
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.CommentDraft,
                    OwnerId = "admin-1",
                    PendingCommentId = CommentId,
                    IsPublished = false,
                });
            this.Handler = new UpdateCommentCommandHandler(
                this.Comments.Object,
                this.Sanitizer.Object,
                this.Users.Object,
                new CommentImageManager(this.Images.Object));
            this.Command = new UpdateCommentCommand(
                "admin-1",
                CommentId,
                new CommentEditModel(
                    new[] { new LocalizedTextValue("fr", this.UpdatedHtml) },
                    false,
                    this.InitialRevision));
        }

        public long InitialRevision { get; } = 4;

        public CancellationToken OperationToken => this.operationCancellation.Token;

        public string UpdatedHtml { get; }

        public Comment Existing { get; }

        public Mock<ICommentRepository> Comments { get; } =
            new Mock<ICommentRepository>(MockBehavior.Strict);

        public Mock<ICommentContentSanitizer> Sanitizer { get; } =
            new Mock<ICommentContentSanitizer>(MockBehavior.Strict);

        public Mock<IUserRepository> Users { get; } =
            new Mock<IUserRepository>(MockBehavior.Strict);

        public Mock<IImageRepository> Images { get; } =
            new Mock<IImageRepository>(MockBehavior.Strict);

        public UpdateCommentCommandHandler Handler { get; }

        public UpdateCommentCommand Command { get; }

        public string? ReservationToken { get; private set; }

        public void SetupUpdateFailure(
            Exception exception,
            Action<Comment, long> beforeThrow)
        {
            this.Comments
                .Setup(repository => repository.UpdateAsync(
                    this.Existing,
                    this.InitialRevision,
                    this.OperationToken))
                .Callback((
                    Comment comment,
                    long expectedRevision,
                    CancellationToken _) =>
                    beforeThrow(comment, expectedRevision))
                .ThrowsAsync(exception);
        }

        public void SetupRecovery(Func<Comment?> outcome)
        {
            this.Comments
                .Setup(repository => repository.GetByIdAsync(
                    CommentId,
                    CancellationToken.None))
                .ReturnsAsync(outcome);
        }

        public void SetupFinalization()
        {
            this.Images
                .Setup(repository => repository.FinalizeCommentDraftAsync(
                    DraftImageId,
                    "admin-1",
                    CommentId,
                    It.Is<string>(token => token == this.ReservationToken),
                    CancellationToken.None))
                .ReturnsAsync(new Image
                {
                    Id = DraftImageId,
                    Category = ImageCategory.Comment,
                    OwnerType = ImageOwnerType.Comment,
                    OwnerId = CommentId,
                    IsPublished = true,
                });
        }

        public Comment CreateRecoveryCandidate(
            long revision,
            bool useUpdatedContent)
        {
            return new Comment
            {
                Id = this.Existing.Id,
                TargetType = this.Existing.TargetType,
                TargetId = this.Existing.TargetId,
                ParkId = this.Existing.ParkId,
                AuthorUserId = this.Existing.AuthorUserId,
                AuthorDisplayName = this.Existing.AuthorDisplayName,
                AuthorAvatarUrl = this.Existing.AuthorAvatarUrl,
                AuthorRole = this.Existing.AuthorRole,
                Bodies = useUpdatedContent
                    ? this.Existing.Bodies
                        .Select(static body => new LocalizedText(
                            body.LanguageCode,
                            body.Value))
                        .ToList()
                    : new List<LocalizedText>
                    {
                        new LocalizedText("fr", "<p>Other</p>"),
                    },
                ImageIds = this.Existing.ImageIds.ToList(),
                Revision = revision,
                IsOfficial = this.Existing.IsOfficial,
                ModerationStatus = this.Existing.ModerationStatus,
                CreatedAtUtc = this.Existing.CreatedAtUtc,
                UpdatedAtUtc = this.Existing.UpdatedAtUtc,
            };
        }

        public void VerifyAll()
        {
            this.Comments.VerifyAll();
            this.Sanitizer.VerifyAll();
            this.Users.VerifyAll();
            this.Images.VerifyAll();
        }

        public void VerifyReservationWasNotReleased()
        {
            this.Images.Verify(repository => repository.ReleaseCommentDraftReservationAsync(
                DraftImageId,
                "admin-1",
                CommentId,
                It.IsAny<string>(),
                CancellationToken.None), Times.Never);
        }

        public void VerifyReservationWasNotFinalized()
        {
            this.Images.Verify(repository => repository.FinalizeCommentDraftAsync(
                DraftImageId,
                "admin-1",
                CommentId,
                It.IsAny<string>(),
                CancellationToken.None), Times.Never);
        }

        public void Dispose()
        {
            this.operationCancellation.Dispose();
        }
    }
}
