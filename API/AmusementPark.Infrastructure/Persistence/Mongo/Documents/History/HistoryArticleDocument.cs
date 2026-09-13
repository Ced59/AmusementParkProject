using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

public sealed class HistoryArticleDocument
{
    [BsonElement("slug")]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new();

    [BsonElement("subtitles")]
    public List<LocalizedTextDocument> Subtitles { get; set; } = new();

    [BsonElement("summaries")]
    public List<LocalizedTextDocument> Summaries { get; set; } = new();

    [BsonElement("mainImageId")]
    [BsonIgnoreIfNull]
    public string? MainImageId { get; set; }

    [BsonElement("blocks")]
    public List<HistoryArticleBlockDocument> Blocks { get; set; } = new();

    [BsonElement("sources")]
    public List<HistorySourceReferenceDocument> Sources { get; set; } = new();

    [BsonElement("isPublished")]
    public bool IsPublished { get; set; } = true;
}
