using AmusementPark.Core.Domain.Ratings;
using Xunit;

namespace AmusementPark.Core.Tests.Domain.Ratings;

public sealed class UserRankingShareTests
{
    private static readonly DateTime InitialUtc = new DateTime(2026, 8, 20, 18, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Create_ShouldKeepTheRankingPrivate()
    {
        UserRankingShare share = UserRankingShare.Create(" user-1 ", InitialUtc);

        Assert.Equal("user-1", share.UserId);
        Assert.False(share.IsPublic);
        Assert.Null(share.ShareId);
        Assert.Null(share.PublishedAtUtc);
    }

    [Fact]
    public void Publish_WhenAlreadyPublic_ShouldKeepTheExistingLink()
    {
        UserRankingShare share = UserRankingShare.Create("user-1", InitialUtc);
        share.Publish("first-share-id", InitialUtc);

        share.Publish("replacement-share-id", InitialUtc.AddHours(1));

        Assert.True(share.IsPublic);
        Assert.Equal("first-share-id", share.ShareId);
        Assert.Equal(InitialUtc, share.PublishedAtUtc);
    }

    [Fact]
    public void Revoke_ShouldImmediatelyInvalidateThePublicIdentifier()
    {
        UserRankingShare share = UserRankingShare.Create("user-1", InitialUtc);
        share.Publish("first-share-id", InitialUtc);

        share.Revoke(InitialUtc.AddHours(1));

        Assert.False(share.IsPublic);
        Assert.Null(share.ShareId);
        Assert.Null(share.PublishedAtUtc);
        Assert.Equal(InitialUtc.AddHours(1), share.UpdatedAtUtc);
    }

    [Fact]
    public void Publish_AfterRevocation_ShouldUseANewIdentifier()
    {
        UserRankingShare share = UserRankingShare.Create("user-1", InitialUtc);
        share.Publish("first-share-id", InitialUtc);
        share.Revoke(InitialUtc.AddHours(1));

        share.Publish("new-share-id", InitialUtc.AddHours(2));

        Assert.True(share.IsPublic);
        Assert.Equal("new-share-id", share.ShareId);
        Assert.Equal(InitialUtc.AddHours(2), share.PublishedAtUtc);
    }
}
