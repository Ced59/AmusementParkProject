using AmusementPark.Application.Features.Sharing.Services;
using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Migrations;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Migrations;

public sealed class PersonalRankingShareReplacementMigrationTests
{
    private const string TokenValue = "AAECAwQFBgcICQoLDA0ODxAREhMUFRYXGBkaGxwdHh8";
    private static readonly DateTime CreatedAtUtc = new DateTime(2026, 8, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime PublishedAtUtc = new DateTime(2026, 8, 2, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void BuildPublication_ForPublicLegacyShare_ShouldPreserveItsPublicLink()
    {
        LegacyUserRankingShareDocument legacy = CreateLegacy(isPublic: true);

        SharePublication publication = PersonalRankingShareReplacementMigration.BuildPublication(
            legacy,
            CreatePolicy(),
            7);

        Assert.Equal("owner-1", publication.OwnerUserId);
        Assert.Equal(PersonalRankingShareSourceScope.Create("owner-1"), publication.SourceScopeKey);
        Assert.Equal(SharePublicationStatus.Published, publication.Status);
        Assert.Equal(ShareVisibility.Unlisted, publication.Visibility);
        Assert.Equal(TokenValue, publication.ShareToken!.Value.Value);
        Assert.Equal(PublishedAtUtc, publication.PublishedAtUtc);
        Assert.Equal(7, publication.SourceVersion);
        Assert.True(publication.ContentPolicy.Includes(ShareContentField.GlobalRatings));
        Assert.False(publication.ContentPolicy.Includes(ShareContentField.Avatar));
    }

    [Fact]
    public void BuildPublication_ForPrivateLegacyShare_ShouldNotCarryAResidualToken()
    {
        LegacyUserRankingShareDocument legacy = CreateLegacy(isPublic: false);
        legacy.ShareId = TokenValue;
        legacy.PublishedAtUtc = PublishedAtUtc;

        SharePublication publication = PersonalRankingShareReplacementMigration.BuildPublication(
            legacy,
            CreatePolicy(),
            0);

        Assert.Equal(SharePublicationStatus.Draft, publication.Status);
        Assert.Equal(ShareVisibility.Private, publication.Visibility);
        Assert.Null(publication.ShareToken);
        Assert.Null(publication.PublishedAtUtc);
        Assert.False(publication.IsResolvable);
    }

    [Fact]
    public void CreateDeterministicPublicationId_ShouldBeStableAndNamespaced()
    {
        SharePublicationId first = PersonalRankingShareReplacementMigration
            .CreateDeterministicPublicationId("legacy-1");
        SharePublicationId repeated = PersonalRankingShareReplacementMigration
            .CreateDeterministicPublicationId("legacy-1");
        SharePublicationId different = PersonalRankingShareReplacementMigration
            .CreateDeterministicPublicationId("legacy-2");

        Assert.Equal(first, repeated);
        Assert.NotEqual(first, different);
        Assert.Equal(64, first.Value.Length);
    }

    [Fact]
    public void BuildPublication_ForInvalidPublicLegacyShare_ShouldBlockCutover()
    {
        LegacyUserRankingShareDocument legacy = CreateLegacy(isPublic: true);
        legacy.ShareId = "not-a-public-token";

        InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() =>
            PersonalRankingShareReplacementMigration.BuildPublication(
                legacy,
                CreatePolicy(),
                0));

        Assert.Contains("valid token", exception.Message, StringComparison.Ordinal);
    }

    private static LegacyUserRankingShareDocument CreateLegacy(bool isPublic)
    {
        return new LegacyUserRankingShareDocument
        {
            Id = "legacy-1",
            UserId = "owner-1",
            IsPublic = isPublic,
            ShareId = isPublic ? TokenValue : null,
            PublishedAtUtc = isPublic ? PublishedAtUtc : null,
            CreatedAt = CreatedAtUtc,
            UpdatedAt = PublishedAtUtc,
        };
    }

    private static ShareContentPolicy CreatePolicy()
    {
        return ShareContentPolicy.Create(
            SharePublicationType.PersonalRanking,
            ShareDatePrecision.Hidden,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.GlobalRatings,
            });
    }
}
