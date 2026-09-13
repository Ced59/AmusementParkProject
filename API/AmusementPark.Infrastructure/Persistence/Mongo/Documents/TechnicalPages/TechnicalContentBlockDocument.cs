using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

public sealed class TechnicalContentBlockDocument
{
    [BsonElement("blockType")]
    public string BlockType { get; set; } = "richText";

    [BsonElement("tone")]
    [BsonIgnoreIfNull]
    public string? Tone { get; set; }

    [BsonElement("imageUrl")]
    [BsonIgnoreIfNull]
    public string? ImageUrl { get; set; }

    [BsonElement("imageId")]
    [BsonIgnoreIfNull]
    public string? ImageId { get; set; }

    [BsonElement("diagramKey")]
    [BsonIgnoreIfNull]
    public string? DiagramKey { get; set; }

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new();

    [BsonElement("bodies")]
    public List<LocalizedTextDocument> Bodies { get; set; } = new();

    [BsonElement("captions")]
    public List<LocalizedTextDocument> Captions { get; set; } = new();

    [BsonElement("altTexts")]
    public List<LocalizedTextDocument> AltTexts { get; set; } = new();

    [BsonElement("items")]
    public List<TechnicalContentListItemDocument> Items { get; set; } = new();

    [BsonElement("table")]
    [BsonIgnoreIfNull]
    public TechnicalContentTableDocument? Table { get; set; }

    [BsonElement("metrics")]
    public List<TechnicalContentMetricDocument> Metrics { get; set; } = new();

    [BsonElement("links")]
    public List<TechnicalContentLinkDocument> Links { get; set; } = new();

    [BsonElement("columns")]
    public List<TechnicalContentBlockDocument> Columns { get; set; } = new();
}
