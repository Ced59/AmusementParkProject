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

internal sealed class CreateCommentWithImageScenario
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
            It.Is<long>(revision => revision == 0),
            It.IsAny<DateTime>(),
            It.IsAny<CancellationToken>()))
            .Callback((
                string _,
                string _,
                string commentId,
                string reservationToken,
                long _,
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
