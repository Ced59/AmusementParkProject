using AmusementPark.Application.Errors;
using AmusementPark.Application.Features.Sharing.Models;
using AmusementPark.Application.Features.Sharing.Ports;
using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using Moq;
using Xunit;

namespace AmusementPark.Application.Tests.Features.Sharing.Services;

public sealed class PersonalRankingSharePublicationSourceTests
{
    private static readonly DateTime Now = new DateTime(2026, 9, 6, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public async Task GetCurrentSourceVersionAsync_ShouldCombineOwnerAndCatalogRevisions()
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.Is<IReadOnlyCollection<string>>(keys =>
                    keys.Count == 2
                    && keys.Contains("personal-ranking:owner-1")
                    && keys.Contains(PersonalRankingShareSourceScope.PublicCatalog)),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                ["personal-ranking:owner-1"] = new ShareSourceRevision(4, 0, Now),
                [PersonalRankingShareSourceScope.PublicCatalog] = new ShareSourceRevision(3, 0, Now),
            });
        PersonalRankingSharePublicationSource source =
            new PersonalRankingSharePublicationSource(revisions.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(7, result.Value);
        ShareContentPolicy policy = source.CreateDefaultPolicy();
        Assert.True(policy.Includes(ShareContentField.PublicDisplayName));
        Assert.False(policy.Includes(ShareContentField.Avatar));
        Assert.True(policy.Includes(ShareContentField.GlobalRatings));
        revisions.VerifyAll();
    }

    [Fact]
    public async Task GetCurrentSourceVersionAsync_WhenAMutationIsPending_ShouldRejectSnapshot()
    {
        Mock<IShareSourceRevisionRepository> revisions =
            new Mock<IShareSourceRevisionRepository>(MockBehavior.Strict);
        revisions.Setup(value => value.GetSnapshotAsync(
                It.IsAny<IReadOnlyCollection<string>>(),
                CancellationToken.None))
            .ReturnsAsync(new Dictionary<string, ShareSourceRevision>(StringComparer.Ordinal)
            {
                ["personal-ranking:owner-1"] = new ShareSourceRevision(4, 1, Now),
                [PersonalRankingShareSourceScope.PublicCatalog] = new ShareSourceRevision(3, 0, Now),
            });
        PersonalRankingSharePublicationSource source =
            new PersonalRankingSharePublicationSource(revisions.Object);

        ApplicationResult<long> result = await source.GetCurrentSourceVersionAsync(
            "personal-ranking:owner-1",
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains(result.Errors, static error => error.Code == "share-publication.source-changed");
        revisions.VerifyAll();
    }

    [Fact]
    public void ValidatePolicyForPublication_ShouldRequireGlobalRatings()
    {
        PersonalRankingSharePublicationSource source = new PersonalRankingSharePublicationSource(
            Mock.Of<IShareSourceRevisionRepository>());
        ShareContentPolicy identityOnlyPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[] { ShareContentField.PublicDisplayName });

        ApplicationResult<bool> result = source.ValidatePolicyForPublication(identityOnlyPolicy);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "share-publication.required-content-missing");
    }

    [Fact]
    public void ValidatePolicyForPublication_WhenAvatarIsSelected_ShouldRejectUnsupportedContent()
    {
        PersonalRankingSharePublicationSource source = new PersonalRankingSharePublicationSource(
            Mock.Of<IShareSourceRevisionRepository>());
        ShareContentPolicy avatarPolicy = ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.Avatar,
                ShareContentField.GlobalRatings,
            });

        ApplicationResult<bool> result = source.ValidatePolicyForPublication(avatarPolicy);

        Assert.False(result.IsSuccess);
        Assert.Contains(
            result.Errors,
            static error => error.Code == "share-publication.content-not-supported");
    }
}
