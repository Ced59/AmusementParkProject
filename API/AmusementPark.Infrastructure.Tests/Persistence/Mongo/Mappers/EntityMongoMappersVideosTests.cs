using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Mappers;
using MongoDB.Bson;
using Xunit;

namespace AmusementPark.Infrastructure.Tests.Persistence.Mongo.Mappers;

public sealed class EntityMongoMappersVideosTests
{
    [Fact]
    public void ToVideoDomain_WhenDocumentUsesHttpEnumTokens_ShouldReadVideo()
    {
        DateTime now = new DateTime(2026, 6, 17, 12, 0, 0, DateTimeKind.Utc);
        BsonDocument document = new BsonDocument
        {
            { "_id", "video-1" },
            { "hostingProvider", "YOUTUBE" },
            { "ownerType", "PARK_ITEM" },
            { "ownerId", "item-1" },
            { "type", "ON_RIDE" },
            { "originalUrl", "https://youtube.com/watch?v=abcdefghijk" },
            { "canonicalUrl", "https://www.youtube.com/watch?v=abcdefghijk" },
            { "durationSeconds", 123L },
            { "languageCodes", new BsonArray { "fr", "fr", "en" } },
            { "titles", new BsonArray
                {
                    new BsonDocument
                    {
                        { "languageCode", "fr" },
                        { "value", "River Quest onride" },
                    },
                }
            },
            { "externalMetadata", new BsonDocument
                {
                    { "providerChannelId", "channel-1" },
                    { "providerChannelUrl", "https://www.youtube.com/channel/channel-1" },
                    { "fetchedAtUtc", new BsonDateTime(now) },
                }
            },
            { "isPublished", true },
            { "createdAt", new BsonDateTime(now) },
            { "updatedAt", new BsonDateTime(now) },
        };

        Video video = EntityMongoMappers.ToVideoDomain(document);

        Assert.Equal("video-1", video.Id);
        Assert.Equal(VideoHostingProvider.YouTube, video.HostingProvider);
        Assert.Equal(VideoOwnerType.ParkItem, video.OwnerType);
        Assert.Equal(VideoType.OnRide, video.Type);
        Assert.Equal(new[] { "fr", "en" }, video.LanguageCodes);
        Assert.Equal(TimeSpan.FromSeconds(123), video.Duration);
        Assert.Equal("channel-1", video.ExternalMetadata.ProviderChannelId);
        Assert.Single(video.Titles);
    }

    [Fact]
    public void ToVideoTagDomain_WhenDocumentUsesObjectId_ShouldReadTag()
    {
        ObjectId id = ObjectId.GenerateNewId();
        BsonDocument document = new BsonDocument
        {
            { "_id", id },
            { "slug", "official-amusementparks" },
            { "labels", new BsonArray
                {
                    new BsonDocument
                    {
                        { "languageCode", "fr" },
                        { "value", "Officiel AmusementParks.fun" },
                    },
                }
            },
            { "isActive", true },
        };

        VideoTag tag = EntityMongoMappers.ToVideoTagDomain(document);

        Assert.Equal(id.ToString(), tag.Id);
        Assert.Equal("official-amusementparks", tag.Slug);
        Assert.True(tag.IsActive);
        Assert.Single(tag.Labels);
        Assert.Empty(tag.Descriptions);
    }
}
