using AmusementPark.Core.Domain.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Sharing;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Repositories;

public sealed class SharePublicationMongoMapperTests
{
    private const string TokenValue =
        "AQIDBAUGBwgJCgsMDQ4PEBESExQVFhcYGRobHB0eHyA";

    private static readonly DateTime InitialUtc =
        new DateTime(2026, 9, 5, 21, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Mapping_ShouldRoundTripEveryLifecycleState()
    {
        IReadOnlyCollection<SharePublication> publications = CreateLifecycleStates();

        foreach (SharePublication publication in publications)
        {
            SharePublicationDocument document = publication.ToDocument();
            SharePublication restored = document.ToDomain();

            Assert.Equal(publication.Id, restored.Id);
            Assert.Equal(publication.OwnerUserId, restored.OwnerUserId);
            Assert.Equal(publication.Type, restored.Type);
            Assert.Equal(publication.SourceScopeKey, restored.SourceScopeKey);
            Assert.Equal(publication.ShareToken, restored.ShareToken);
            Assert.Equal(publication.Status, restored.Status);
            Assert.Equal(publication.Visibility, restored.Visibility);
            Assert.True(publication.ContentPolicy.HasSameSelectionAs(restored.ContentPolicy));
            Assert.Equal(publication.SourceVersion, restored.SourceVersion);
            Assert.Equal(publication.PublicationVersion, restored.PublicationVersion);
            Assert.Equal(publication.Version, restored.Version);
            Assert.Equal(publication.PublishedAtUtc, restored.PublishedAtUtc);
            Assert.Equal(publication.RevokedAtUtc, restored.RevokedAtUtc);
            Assert.Equal(publication.CreatedAtUtc, restored.CreatedAtUtc);
            Assert.Equal(publication.UpdatedAtUtc, restored.UpdatedAtUtc);
        }
    }

    [Fact]
    public void ToDocument_ShouldPersistOnlyTheSharingWhitelistAndInternalLifecycleData()
    {
        SharePublication publication = CreateDraft();
        BsonDocument bson = publication.ToDocument().ToBsonDocument();
        string serialized = bson.ToJson();

        Assert.Contains("contentPolicy", serialized, StringComparison.Ordinal);
        Assert.Contains("includedFields", serialized, StringComparison.Ordinal);
        Assert.DoesNotContain("privateComment", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("privateNote", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("companion", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("email", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("latitude", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("longitude", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("location", serialized, StringComparison.OrdinalIgnoreCase);
        Assert.False(bson.Contains("shareToken"));
        Assert.False(bson.Contains("publishedAtUtc"));
        Assert.False(bson.Contains("revokedAtUtc"));
    }

    private static IReadOnlyCollection<SharePublication> CreateLifecycleStates()
    {
        SharePublication draft = CreateDraft();
        SharePublication published = CreatePublished();
        SharePublication needsReview = CreatePublished();
        needsReview.MarkSourceChanged(8, InitialUtc.AddMinutes(2));
        SharePublication revoked = CreatePublished();
        revoked.Revoke(1, InitialUtc.AddMinutes(2));
        return new[] { draft, published, needsReview, revoked };
    }

    private static SharePublication CreateDraft()
    {
        ShareContentPolicy policy = ShareContentPolicy.Create(
            SharePublicationType.PassportProfile,
            ShareDatePrecision.Year,
            new[]
            {
                ShareContentField.PublicDisplayName,
                ShareContentField.RideCount,
                ShareContentField.GeographicStatistics,
            });
        return SharePublication.Create(
            SharePublicationId.Parse("publication-1"),
            "user-1",
            SharePublicationType.PassportProfile,
            "passport:user-1",
            policy,
            7,
            InitialUtc);
    }

    private static SharePublication CreatePublished()
    {
        SharePublication publication = CreateDraft();
        publication.Publish(
            ShareToken.Parse(TokenValue),
            ShareVisibility.Unlisted,
            7,
            publication.ContentPolicy,
            0,
            InitialUtc.AddMinutes(1));
        return publication;
    }
}
