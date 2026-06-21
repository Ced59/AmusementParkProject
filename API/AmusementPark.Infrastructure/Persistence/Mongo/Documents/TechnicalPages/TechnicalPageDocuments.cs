using AmusementPark.Core.Domain.Parks;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.TechnicalPages;

[BsonIgnoreExtraElements]
public sealed class TechnicalPageDocument : MongoDocumentBase
{
    [BsonElement("categoryKey")]
    public string CategoryKey { get; set; } = string.Empty;

    [BsonElement("categoryNames")]
    public List<LocalizedTextDocument> CategoryNames { get; set; } = new();

    [BsonElement("slug")]
    public string Slug { get; set; } = string.Empty;

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new();

    [BsonElement("summaries")]
    public List<LocalizedTextDocument> Summaries { get; set; } = new();

    [BsonElement("aliases")]
    public List<TechnicalPageAliasDocument> Aliases { get; set; } = new();

    [BsonElement("contentBlocks")]
    public List<TechnicalContentBlockDocument> ContentBlocks { get; set; } = new();

    [BsonElement("sortOrder")]
    public int SortOrder { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; } = true;

    [BsonElement("adminReviewStatus")]
    [BsonRepresentation(BsonType.String)]
    public AdminReviewStatus AdminReviewStatus { get; set; } = AdminReviewStatus.ToReview;

    [BsonElement("adminReviewPriority")]
    public int AdminReviewPriority { get; set; }
}

public sealed class TechnicalPageAliasDocument
{
    [BsonElement("categoryKey")]
    public string CategoryKey { get; set; } = string.Empty;

    [BsonElement("labels")]
    public List<LocalizedTextDocument> Labels { get; set; } = new();
}

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

public sealed class TechnicalContentListItemDocument
{
    [BsonElement("texts")]
    public List<LocalizedTextDocument> Texts { get; set; } = new();
}

public sealed class TechnicalContentTableDocument
{
    [BsonElement("headers")]
    public List<TechnicalContentTableCellDocument> Headers { get; set; } = new();

    [BsonElement("rows")]
    public List<TechnicalContentTableRowDocument> Rows { get; set; } = new();
}

public sealed class TechnicalContentTableRowDocument
{
    [BsonElement("cells")]
    public List<TechnicalContentTableCellDocument> Cells { get; set; } = new();
}

public sealed class TechnicalContentTableCellDocument
{
    [BsonElement("texts")]
    public List<LocalizedTextDocument> Texts { get; set; } = new();
}

public sealed class TechnicalContentMetricDocument
{
    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();

    [BsonElement("value")]
    public List<LocalizedTextDocument> Value { get; set; } = new();

    [BsonElement("helpText")]
    public List<LocalizedTextDocument> HelpText { get; set; } = new();
}

public sealed class TechnicalContentLinkDocument
{
    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("label")]
    public List<LocalizedTextDocument> Label { get; set; } = new();
}
