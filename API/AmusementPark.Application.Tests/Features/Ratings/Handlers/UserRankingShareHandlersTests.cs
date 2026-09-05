using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Ratings.Commands;
using AmusementPark.Application.Features.Ratings.Handlers;
using AmusementPark.Application.Features.Ratings.Ports;
using AmusementPark.Application.Features.Ratings.Queries;
using AmusementPark.Application.Features.Ratings.Results;
using AmusementPark.Application.Features.Ratings.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Ratings;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Ratings.Handlers;

public sealed class UserRankingShareHandlersTests
{
    private const string ShareId = "abcdefghijklmnopqrstuvwxyz0123456789_ABCD";
    private static readonly DateTimeOffset Now = new DateTimeOffset(2026, 8, 20, 18, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task SetVisibility_WhenPublishing_ShouldCreateAnOpaquePublicLinkForTheRequestedUser()
    {
        Mock<IUserRankingShareRepository> repository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        repository
            .Setup(candidate => candidate.GetByUserIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRankingShare?)null);
        repository
            .Setup(candidate => candidate.UpsertAsync(
                It.Is<UserRankingShare>(share =>
                    share.UserId == "owner-1"
                    && share.IsPublic
                    && share.ShareId == ShareId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRankingShare share, CancellationToken _) => share);
        Mock<IUserRankingShareIdFactory> idFactory = new Mock<IUserRankingShareIdFactory>(MockBehavior.Strict);
        idFactory.Setup(candidate => candidate.Generate()).Returns(ShareId);
        SetUserRankingShareVisibilityCommandHandler handler = new SetUserRankingShareVisibilityCommandHandler(
            repository.Object,
            idFactory.Object,
            new UserRankingShareFixedTimeProvider(Now));

        ApplicationResult<UserRankingShareSettingsResult> result = await handler.HandleAsync(
            new SetUserRankingShareVisibilityCommand(" owner-1 ", true));

        Assert.True(result.IsSuccess);
        Assert.True(result.Value!.IsPublic);
        Assert.Equal(ShareId, result.Value.ShareId);
        Assert.Equal(Now.UtcDateTime, result.Value.PublishedAtUtc);
        repository.VerifyAll();
        idFactory.VerifyAll();
    }

    [Fact]
    public async Task SetVisibility_WhenRevoking_ShouldRemoveTheOldPublicLink()
    {
        UserRankingShare share = UserRankingShare.Create("owner-1", Now.UtcDateTime.AddDays(-1));
        share.Publish(ShareId, Now.UtcDateTime.AddDays(-1));
        Mock<IUserRankingShareRepository> repository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        repository
            .Setup(candidate => candidate.GetByUserIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);
        repository
            .Setup(candidate => candidate.UpsertAsync(
                It.Is<UserRankingShare>(candidate => !candidate.IsPublic && candidate.ShareId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((UserRankingShare candidate, CancellationToken _) => candidate);
        Mock<IUserRankingShareIdFactory> idFactory = new Mock<IUserRankingShareIdFactory>(MockBehavior.Strict);
        SetUserRankingShareVisibilityCommandHandler handler = new SetUserRankingShareVisibilityCommandHandler(
            repository.Object,
            idFactory.Object,
            new UserRankingShareFixedTimeProvider(Now));

        ApplicationResult<UserRankingShareSettingsResult> result = await handler.HandleAsync(
            new SetUserRankingShareVisibilityCommand("owner-1", false));

        Assert.True(result.IsSuccess);
        Assert.False(result.Value!.IsPublic);
        Assert.Null(result.Value.ShareId);
        Assert.Null(result.Value.PublishedAtUtc);
        repository.VerifyAll();
        idFactory.Verify(candidate => candidate.Generate(), Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnerIsBlocked_ShouldExposeNoPublicProfile()
    {
        UserRankingShare share = UserRankingShare.Create("owner-1", Now.UtcDateTime.AddDays(-1));
        share.Publish(ShareId, Now.UtcDateTime.AddDays(-1));
        User owner = new User
        {
            Id = "owner-1",
            IsActivated = true,
            IsBlocked = true,
            PublicDisplayName = "Hidden owner",
        };
        Mock<IUserRankingShareRepository> shareRepository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        shareRepository
            .Setup(candidate => candidate.GetPublicByShareIdAsync(ShareId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(candidate => candidate.GetByIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        UserRankingShareAccessResolver resolver = new UserRankingShareAccessResolver(
            shareRepository.Object,
            userRepository.Object);

        ApplicationResult<UserRankingShareOwner> result = await resolver.ResolveAsync(ShareId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
        shareRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Fact]
    public async Task GetSharedProfile_ShouldCalculateStatsFromVisibleTargetsOnly()
    {
        UserRankingShare share = UserRankingShare.Create("owner-1", Now.UtcDateTime.AddDays(-1));
        share.Publish(ShareId, Now.UtcDateTime.AddDays(-1));
        User owner = new User
        {
            Id = "owner-1",
            IsActivated = true,
            PublicDisplayName = "Camille",
        };
        Mock<IUserRankingShareRepository> shareRepository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        shareRepository
            .Setup(candidate => candidate.GetPublicByShareIdAsync(ShareId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(candidate => candidate.GetByIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        Mock<IRatingRepository> ratingRepository = new Mock<IRatingRepository>(MockBehavior.Strict);
        UserRatingStatsResult stats = new UserRatingStatsResult(
            0,
            0d,
            0d,
            0d,
            Array.Empty<UserRatingStatBucketResult>(),
            Array.Empty<UserRatingStatBucketResult>(),
            Array.Empty<UserRatingStatBucketResult>());
        ratingRepository
            .Setup(candidate => candidate.GetVisibleUserRatingStatsAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(stats);
        GetSharedUserRankingProfileQueryHandler handler = new GetSharedUserRankingProfileQueryHandler(
            new UserRankingShareAccessResolver(shareRepository.Object, userRepository.Object),
            ratingRepository.Object);

        ApplicationResult<SharedUserRankingProfileResult> result = await handler.HandleAsync(
            new GetSharedUserRankingProfileQuery(ShareId));

        Assert.True(result.IsSuccess);
        Assert.Equal("Camille", result.Value!.DisplayName);
        ratingRepository.VerifyAll();
        ratingRepository.Verify(
            candidate => candidate.GetUserRatingStatsAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task ResolveAsync_WhenPublicNicknameIsMissing_ShouldFallbackToTheTechnicalIdentifier()
    {
        UserRankingShare share = UserRankingShare.Create("owner-1", Now.UtcDateTime.AddDays(-1));
        share.Publish(ShareId, Now.UtcDateTime.AddDays(-1));
        User owner = new User
        {
            Id = "owner-1",
            IsActivated = true,
            Roles = new List<Role> { Role.User, Role.Admin },
        };
        owner.AssignPublicAccountNumber(1);
        Mock<IUserRankingShareRepository> shareRepository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        shareRepository
            .Setup(candidate => candidate.GetPublicByShareIdAsync(ShareId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(share);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        userRepository
            .Setup(candidate => candidate.GetByIdAsync("owner-1", It.IsAny<CancellationToken>()))
            .ReturnsAsync(owner);
        UserRankingShareAccessResolver resolver = new UserRankingShareAccessResolver(
            shareRepository.Object,
            userRepository.Object);

        ApplicationResult<UserRankingShareOwner> result = await resolver.ResolveAsync(
            ShareId,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Admin01", result.Value!.DisplayName);
        shareRepository.VerifyAll();
        userRepository.VerifyAll();
    }

    [Theory]
    [InlineData("")]
    [InlineData("short")]
    [InlineData("abcdefghijklmnopqrstuvwxyz0123456789+invalid")]
    public async Task ResolveAsync_WhenShareIdentifierIsInvalid_ShouldNotQueryPersistence(string shareId)
    {
        Mock<IUserRankingShareRepository> shareRepository = new Mock<IUserRankingShareRepository>(MockBehavior.Strict);
        Mock<IUserRepository> userRepository = new Mock<IUserRepository>(MockBehavior.Strict);
        UserRankingShareAccessResolver resolver = new UserRankingShareAccessResolver(
            shareRepository.Object,
            userRepository.Object);

        ApplicationResult<UserRankingShareOwner> result = await resolver.ResolveAsync(shareId, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
        shareRepository.VerifyNoOtherCalls();
        userRepository.VerifyNoOtherCalls();
    }

}
