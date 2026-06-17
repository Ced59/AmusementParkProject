using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;

[BsonIgnoreExtraElements]
public sealed class VideoDocument : MongoDocumentBase
{
    [BsonElement("hostingProvider")]
    [BsonRepresentation(BsonType.String)]
    public VideoHostingProvider HostingProvider { get; set; } = VideoHostingProvider.Other;

    [BsonElement("ownerType")]
    [BsonRepresentation(BsonType.String)]
    public VideoOwnerType OwnerType { get; set; } = VideoOwnerType.None;

    [BsonElement("ownerId")]
    [BsonIgnoreIfNull]
    public string? OwnerId { get; set; }

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public VideoType Type { get; set; } = VideoType.Other;

    [BsonElement("originalUrl")]
    public string OriginalUrl { get; set; } = string.Empty;

    [BsonElement("canonicalUrl")]
    public string CanonicalUrl { get; set; } = string.Empty;

    [BsonElement("embedUrl")]
    [BsonIgnoreIfNull]
    public string? EmbedUrl { get; set; }

    [BsonElement("externalId")]
    [BsonIgnoreIfNull]
    public string? ExternalId { get; set; }

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("description")]
    [BsonIgnoreIfNull]
    public string? Description { get; set; }

    [BsonElement("creatorName")]
    [BsonIgnoreIfNull]
    public string? CreatorName { get; set; }

    [BsonElement("creatorUrl")]
    [BsonIgnoreIfNull]
    public string? CreatorUrl { get; set; }

    [BsonElement("thumbnailUrl")]
    [BsonIgnoreIfNull]
    public string? ThumbnailUrl { get; set; }

    [BsonElement("thumbnailImageId")]
    [BsonIgnoreIfNull]
    public string? ThumbnailImageId { get; set; }

    [BsonElement("durationSeconds")]
    [BsonIgnoreIfNull]
    public long? DurationSeconds { get; set; }

    [BsonElement("publishedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? PublishedAtUtc { get; set; }

    [BsonElement("languageCodes")]
    public List<string> LanguageCodes { get; set; } = new();

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("tagIds")]
    public List<string> TagIds { get; set; } = new();

    [BsonElement("externalMetadata")]
    public VideoExternalMetadataDocument ExternalMetadata { get; set; } = new();

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;
}

public sealed class VideoExternalMetadataDocument
{
    [BsonElement("source")]
    [BsonIgnoreIfNull]
    public string? Source { get; set; }

    [BsonElement("fetchedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? FetchedAtUtc { get; set; }

    [BsonElement("providerTitle")]
    [BsonIgnoreIfNull]
    public string? ProviderTitle { get; set; }

    [BsonElement("providerDescription")]
    [BsonIgnoreIfNull]
    public string? ProviderDescription { get; set; }

    [BsonElement("providerChannelId")]
    [BsonIgnoreIfNull]
    public string? ProviderChannelId { get; set; }

    [BsonElement("providerChannelUrl")]
    [BsonIgnoreIfNull]
    public string? ProviderChannelUrl { get; set; }
}

[BsonIgnoreExtraElements]
public sealed class VideoTagDocument : MongoDocumentBase
{
    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();

    [BsonElement("descriptions")]
    public List<LocalizedTextDocument> Descriptions { get; set; } = new();

    [BsonElement("isActive")]
    public bool IsActive { get; set; } = true;
}
