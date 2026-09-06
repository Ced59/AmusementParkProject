using System.Text.Json;
using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Images.Ports;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Images;
using AmusementPark.Core.Domain.Parks;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PersonalRankingSharePreviewBuilderTests
{
    private static readonly DateTime NowUtc =
        new DateTime(2026, 9, 5, 22, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task BuildAsync_ShouldExposeOnlyPublicLabelsAndWhitelistedValues()
    {
        Mock<IShareSourceRevisionRepository> revisions = CreateStableRevisions(7);
        Mock<IUserRepository> users = CreateUserRepository();
        Mock<IImageRepository> images = CreateImageRepository(CreateAvatar());
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetVisibleUserRatingStatsAsync(
                "owner-1",
                1000,
                CancellationToken.None))
            .ReturnsAsync(CreateStatistics());
        ratings.Setup(value => value.GetVisibleUserRankingSourcesAsync(
                "owner-1",
                1001,
                CancellationToken.None))
            .ReturnsAsync(new[] { CreateRating() });
        PersonalRankingSharePreviewBuilder builder = new PersonalRankingSharePreviewBuilder(
            revisions.Object,
            ratings.Object,
            users.Object,
            images.Object);
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.Avatar,
                ShareContentField.GlobalRatings,
            });

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            null,
            policy,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value!.SourceVersion);
        PersonalRankingSharePreviewResult preview = Assert.IsType<PersonalRankingSharePreviewResult>(
            result.Value.PersonalRanking);
        Assert.Equal("Camille", preview.DisplayName);
        Assert.Equal("/images/avatar-1", preview.AvatarUrl);
        Assert.Null(Assert.Single(preview.Statistics!.ByPark).Key);
        Assert.Equal("Parc Astérix", Assert.Single(preview.Statistics.ByPark).Label);
        Assert.Equal("ParkItem", Assert.Single(preview.Statistics.ByTargetType).Key);
        Assert.Equal("Attraction", Assert.Single(preview.Statistics.ByParkItemCategory).Key);
        PersonalRankingSharePreviewItemResult item = Assert.Single(preview.Ratings);
        Assert.Equal("OzIris", item.TargetName);
        Assert.Equal("Parc Astérix", item.ParkName);

        string serialized = JsonSerializer.Serialize(result.Value);
        Assert.DoesNotContain("private-rating-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("technical-target-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("technical-park-id", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("owner@example.com", serialized, StringComparison.Ordinal);
        revisions.VerifyAll();
        ratings.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WithPrivateDefault_ShouldNotReadOrExposeRatings()
    {
        Mock<IShareSourceRevisionRepository> revisions = CreateStableRevisions(0);
        Mock<IUserRepository> users = CreateUserRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        PersonalRankingSharePreviewBuilder builder = new PersonalRankingSharePreviewBuilder(
            revisions.Object,
            ratings.Object,
            users.Object,
            images.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            null,
            ShareContentPolicy.CreatePrivateDefault(SharePublicationType.PersonalRanking),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        PersonalRankingSharePreviewResult preview = result.Value!.PersonalRanking!;
        Assert.Equal("User", preview.DisplayName);
        Assert.Null(preview.AvatarUrl);
        Assert.Null(preview.Statistics);
        Assert.Empty(preview.Ratings);
        ratings.VerifyNoOtherCalls();
        revisions.VerifyAll();
        users.VerifyAll();
        images.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildAsync_WhenSourceRevisionChanges_ShouldDiscardThePreview()
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.SetupSequence(value => value.GetOrCreateAsync(
                "personal-ranking:owner-1",
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(2, 0, NowUtc))
            .ReturnsAsync(new ShareSourceRevision(3, 0, NowUtc.AddSeconds(1)));
        revisions.Setup(value => value.GetOrCreateAsync(
                PersonalRankingShareSourceScope.PublicCatalog,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(4, 0, NowUtc));
        Mock<IUserRepository> users = CreateUserRepository();
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        ratings.Setup(value => value.GetVisibleUserRatingStatsAsync(
                "owner-1",
                1000,
                CancellationToken.None))
            .ReturnsAsync(CreateStatistics());
        ratings.Setup(value => value.GetVisibleUserRankingSourcesAsync(
                "owner-1",
                1001,
                CancellationToken.None))
            .ReturnsAsync(new[] { CreateRating() });
        PersonalRankingSharePreviewBuilder builder = new PersonalRankingSharePreviewBuilder(
            revisions.Object,
            ratings.Object,
            users.Object,
            images.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            null,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.GlobalRatings }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.source-changed");
        revisions.VerifyAll();
        ratings.VerifyAll();
        users.VerifyAll();
        images.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BuildAsync_WhenPublicIdentityChangesDuringRead_ShouldDiscardThePreview()
    {
        Mock<IShareSourceRevisionRepository> revisions = CreateStableRevisions(2);
        User before = CreateUser("/avatars/old.webp");
        User after = CreateUser("/avatars/new.webp");
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.SetupSequence(value => value.GetByIdAsync(
                "owner-1",
                CancellationToken.None))
            .ReturnsAsync(before)
            .ReturnsAsync(after);
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = CreateImageRepository(CreateAvatar());
        PersonalRankingSharePreviewBuilder builder = new PersonalRankingSharePreviewBuilder(
            revisions.Object,
            ratings.Object,
            users.Object,
            images.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            null,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.Avatar }),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error =>
            error.Code == "share-publication.source-changed");
        ratings.VerifyNoOtherCalls();
        revisions.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    [Fact]
    public async Task BuildAsync_WhenCurrentAvatarIsUnpublished_ShouldNotExposeIt()
    {
        Mock<IShareSourceRevisionRepository> revisions = CreateStableRevisions(2);
        Mock<IUserRepository> users = CreateUserRepository();
        Mock<IRatingRepository> ratings = new Mock<IRatingRepository>(MockBehavior.Strict);
        Mock<IImageRepository> images = CreateImageRepository(CreateAvatar(isPublished: false));
        PersonalRankingSharePreviewBuilder builder = new PersonalRankingSharePreviewBuilder(
            revisions.Object,
            ratings.Object,
            users.Object,
            images.Object);

        ApplicationResult<SharePublicationPreviewResult> result = await builder.BuildAsync(
            "owner-1",
            null,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.Avatar }),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.PersonalRanking!.AvatarUrl);
        ratings.VerifyNoOtherCalls();
        revisions.VerifyAll();
        users.VerifyAll();
        images.VerifyAll();
    }

    private static Mock<IShareSourceRevisionRepository> CreateStableRevisions(long revision)
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetOrCreateAsync(
                "personal-ranking:owner-1",
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(revision, 0, NowUtc));
        revisions.Setup(value => value.GetOrCreateAsync(
                PersonalRankingShareSourceScope.PublicCatalog,
                CancellationToken.None))
            .ReturnsAsync(new ShareSourceRevision(0, 0, NowUtc));
        return revisions;
    }

    private static Mock<IUserRepository> CreateUserRepository()
    {
        User user = CreateUser("/images/avatar-1");
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(user);
        return users;
    }

    private static Mock<IImageRepository> CreateImageRepository(Image avatar)
    {
        Mock<IImageRepository> images = new Mock<IImageRepository>(MockBehavior.Strict);
        images.Setup(value => value.GetCurrentByOwnerAuthoritativeAsync(
                ImageOwnerType.User,
                "owner-1",
                ImageCategory.Avatar,
                CancellationToken.None))
            .ReturnsAsync(avatar);
        return images;
    }

    private static Image CreateAvatar(bool isPublished = true)
    {
        return new Image
        {
            Id = "avatar-1",
            OwnerType = ImageOwnerType.User,
            OwnerId = "owner-1",
            Category = ImageCategory.Avatar,
            IsCurrent = true,
            IsPublished = isPublished,
        };
    }

    private static User CreateUser(string avatarUrl)
    {
        return new User
        {
            Id = "owner-1",
            Email = "owner@example.com",
            IsActivated = true,
            PublicDisplayName = "Camille",
            AvatarUrl = avatarUrl,
        };
    }

    private static UserRatingStatsResult CreateStatistics()
    {
        return new UserRatingStatsResult(
            1,
            4.5,
            4.5,
            4.5,
            new[] { new UserRatingStatBucketResult("technical-park-id", "Parc Astérix", 1, 4.5) },
            new[] { new UserRatingStatBucketResult("ParkItem", "Attractions", 1, 4.5) },
            new[] { new UserRatingStatBucketResult("Attraction", "Attractions", 1, 4.5) });
    }

    private static UserRatingListItemResult CreateRating()
    {
        return new UserRatingListItemResult(
            "private-rating-id",
            RatingTargetType.ParkItem,
            "technical-target-id",
            "OzIris",
            "technical-park-id",
            "Parc Astérix",
            ParkItemCategory.Attraction,
            ParkItemType.RollerCoaster,
            4.5,
            NowUtc,
            new RatingSummaryResult(
                RatingTargetType.ParkItem,
                "technical-target-id",
                10,
                4.2,
                4.1));
    }
}
