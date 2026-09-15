using AmusementPark.Core.Domain.FactualEvents;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.FactualEvents;

[BsonIgnoreExtraElements]
public sealed class SourceReferenceDocument
{
    [BsonElement("type")]
    public SourceReferenceType Type { get; set; }

    [BsonElement("publisherName")]
    public string PublisherName { get; set; } = string.Empty;

    [BsonElement("title")]
    public string Title { get; set; } = string.Empty;

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("publishedAtUtc")]
    public DateTime PublishedAtUtc { get; set; }
}
