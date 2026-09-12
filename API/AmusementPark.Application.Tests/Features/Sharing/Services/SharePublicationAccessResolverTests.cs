using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Results;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Application.Features.Users.Ports;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Core.Domain.Users;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class SharePublicationAccessResolverTests
{
    private const string TokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime Now = new DateTime(2026, 9, 6, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task ResolveAsync_ForMigratedToken_ShouldReturnTheActiveOwner()
    {
        SharePublication publication = SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            "owner-1",
            SharePublicationType.PersonalRanking,
            "personal-ranking:owner-1",
            ShareToken.Parse(TokenValue),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.PublicDisplayName, ShareContentField.GlobalRatings }),
            0,
            1,
            1,
            Now,
            null,
            Now,
            Now);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        User user = new User
        {
            Id = "owner-1",
            PublicDisplayName = "Coaster Fan",
            IsActivated = true,
            IsBlocked = false,
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(user);
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            publications.Object,
            users.Object,
            CreateSources());

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            TokenValue,
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("owner-1", result.Value!.OwnerUserId);
        Assert.Equal("Coaster Fan", result.Value.DisplayName);
        Assert.Equal(Now, result.Value.PublishedAtUtc);
        publications.Verify(value => value.GetResolvableByTokenAsync(
            ShareToken.Parse(TokenValue),
            CancellationToken.None), Times.Once);
        publications.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenTokenIsMalformed_ShouldReturnNotFoundWithoutReadingStorage()
    {
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            Mock.Of<ISharePublicationRepository>(MockBehavior.Strict),
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            CreateSources());

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            "technical-id",
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
    }

    [Fact]
    public async Task ResolveAsync_WhenOwnerIsBlocked_ShouldExposeNoPublicProfile()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        User user = new User
        {
            Id = "owner-1",
            PublicDisplayName = "Hidden owner",
            IsActivated = true,
            IsBlocked = true,
        };
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(user);
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            publications.Object,
            users.Object,
            CreateSources());

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            TokenValue,
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
        publications.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenDisplayNameWasNotApproved_ShouldReturnAnAnonymousIdentity()
    {
        SharePublication publication = CreatePublishedPublication(includesDisplayName: false);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        Mock<IUserRepository> users = new Mock<IUserRepository>(MockBehavior.Strict);
        users.Setup(value => value.GetByIdAsync("owner-1", CancellationToken.None))
            .ReturnsAsync(new User
            {
                Id = "owner-1",
                PublicDisplayName = "Hidden owner",
                IsActivated = true,
                IsBlocked = false,
            });
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            publications.Object,
            users.Object,
            CreateSources());

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            TokenValue,
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value!.DisplayName);
        publications.VerifyAll();
        users.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenPersistedPolicyCannotRepresentARanking_ShouldExposeNothing()
    {
        SharePublication publication = SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            "owner-1",
            SharePublicationType.PersonalRanking,
            "personal-ranking:owner-1",
            ShareToken.Parse(TokenValue),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                new[] { ShareContentField.PublicDisplayName }),
            0,
            1,
            1,
            Now,
            null,
            Now,
            Now);
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            publications.Object,
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            CreateSources());

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            TokenValue,
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
        publications.VerifyAll();
    }

    [Fact]
    public async Task ResolveAsync_WhenPublishedSourceVersionHasChanged_ShouldExposeNothing()
    {
        SharePublication publication = CreatePublishedPublication();
        Mock<ISharePublicationRepository> publications =
            new Mock<ISharePublicationRepository>(MockBehavior.Strict);
        publications.Setup(value => value.GetResolvableByTokenAsync(
                ShareToken.Parse(TokenValue),
                CancellationToken.None))
            .ReturnsAsync(publication);
        SharePublicationAccessResolver resolver = new SharePublicationAccessResolver(
            publications.Object,
            Mock.Of<IUserRepository>(MockBehavior.Strict),
            CreateSources(ownerRevision: 1));

        ApplicationResult<ResolvedSharePublicationResult> result = await resolver.ResolveAsync(
            TokenValue,
            SharePublicationType.PersonalRanking,
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "rating.shared-ranking.not-found");
        publications.VerifyAll();
    }

    private static SharePublication CreatePublishedPublication(bool includesDisplayName = true)
    {
        return SharePublication.Restore(
            SharePublicationId.Parse("publication-1"),
            "owner-1",
            SharePublicationType.PersonalRanking,
            "personal-ranking:owner-1",
            ShareToken.Parse(TokenValue),
            SharePublicationStatus.Published,
            ShareVisibility.Unlisted,
            ShareContentPolicy.Create(
                SharePublicationType.PersonalRanking,
                ShareDatePrecision.Hidden,
                includesDisplayName
                    ? new[] { ShareContentField.PublicDisplayName, ShareContentField.GlobalRatings }
                    : new[] { ShareContentField.GlobalRatings }),
            0,
            1,
            1,
            Now,
            null,
            Now,
            Now);
    }

    private static IReadOnlyCollection<ISharePublicationSourceDescriptor> CreateSources(
        long ownerRevision = 0)
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                ["personal-ranking:owner-1"] = new ShareSourceRevision(ownerRevision, 0, Now),
                [PublicIdentityShareSourceScope.Create("owner-1")] =
                    new ShareSourceRevision(0, 0, Now),
                [PersonalRankingShareSourceScope.PublicCatalog] = new ShareSourceRevision(0, 0, Now),
            });
        return new[]
        {
            new PersonalRankingSharePublicationSource(revisions.Object),
        };
    }
}
