using AmusementPark.Core.Domain.Videos;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Videos;

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

    [BsonElement("providerViewCount")]
    [BsonIgnoreIfNull]
    public long? ProviderViewCount { get; set; }
}
