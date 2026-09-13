using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoryArticleBlockDocument
{
    [BsonElement("id")]
    public string Id { get; set; } = string.Empty;

    [BsonElement("type")]
    [BsonRepresentation(BsonType.String)]
    public HistoryArticleBlockType Type { get; set; } = HistoryArticleBlockType.Paragraph;

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("headingLevel")]
    [BsonIgnoreIfNull]
    public int? HeadingLevel { get; set; }

    [BsonElement("texts")]
    public List<LocalizedTextDocument> Texts { get; set; } = new();

    [BsonElement("imageId")]
    [BsonIgnoreIfNull]
    public string? ImageId { get; set; }

    [BsonElement("imageIds")]
    public List<string> ImageIds { get; set; } = new();

    [BsonElement("captions")]
    public List<LocalizedTextDocument> Captions { get; set; } = new();
}
