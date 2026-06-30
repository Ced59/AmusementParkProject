using AmusementPark.Core.Domain.History;
using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.History;

[BsonIgnoreExtraElements]
public sealed class HistoryEventDocument : MongoDocumentBase
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("entityType")]
    [BsonRepresentation(BsonType.String)]
    public HistoryEntityType EntityType { get; set; }

    [BsonElement("ownerId")]
    public string OwnerId { get; set; } = string.Empty;

    [BsonElement("parkId")]
    [BsonIgnoreIfNull]
    public string? ParkId { get; set; }

    [BsonElement("parkItemId")]
    [BsonIgnoreIfNull]
    public string? ParkItemId { get; set; }

    [BsonElement("contextParkId")]
    [BsonIgnoreIfNull]
    public string? ContextParkId { get; set; }

    [BsonElement("year")]
    public int Year { get; set; }

    [BsonElement("month")]
    [BsonIgnoreIfNull]
    public int? Month { get; set; }

    [BsonElement("day")]
    [BsonIgnoreIfNull]
    public int? Day { get; set; }

    [BsonElement("datePrecision")]
    [BsonRepresentation(BsonType.String)]
    public HistoryDatePrecision DatePrecision { get; set; } = HistoryDatePrecision.Year;

    [BsonElement("eventType")]
    public string EventType { get; set; } = string.Empty;

    [BsonElement("isMajor")]
    public bool IsMajor { get; set; }

    [BsonElement("isVisible")]
    public bool IsVisible { get; set; } = true;

    [BsonElement("slug")]
    [BsonIgnoreIfNull]
    public string? Slug { get; set; }

    [BsonElement("titles")]
    public List<LocalizedTextDocument> Titles { get; set; } = new();

    [BsonElement("summaries")]
    public List<LocalizedTextDocument> Summaries { get; set; } = new();

    [BsonElement("mainImageId")]
    [BsonIgnoreIfNull]
    public string? MainImageId { get; set; }

    [BsonElement("previousName")]
    [BsonIgnoreIfNull]
    public string? PreviousName { get; set; }

    [BsonElement("newName")]
    [BsonIgnoreIfNull]
    public string? NewName { get; set; }

    [BsonElement("previousLogoImageId")]
    [BsonIgnoreIfNull]
    public string? PreviousLogoImageId { get; set; }

    [BsonElement("newLogoImageId")]
    [BsonIgnoreIfNull]
    public string? NewLogoImageId { get; set; }

    [BsonElement("previousOperatorId")]
    [BsonIgnoreIfNull]
    public string? PreviousOperatorId { get; set; }

    [BsonElement("newOperatorId")]
    [BsonIgnoreIfNull]
    public string? NewOperatorId { get; set; }

    [BsonElement("locationLabel")]
    [BsonIgnoreIfNull]
    public string? LocationLabel { get; set; }

    [BsonElement("relatedParkIds")]
    public List<string> RelatedParkIds { get; set; } = new();

    [BsonElement("relatedParkItemIds")]
    public List<string> RelatedParkItemIds { get; set; } = new();

    [BsonElement("sources")]
    public List<HistorySourceReferenceDocument> Sources { get; set; } = new();

    [BsonElement("article")]
    [BsonIgnoreIfNull]
    public HistoryArticleDocument? Article { get; set; }
}

public sealed class HistorySourceReferenceDocument
{
    [BsonElement("label")]
    [BsonIgnoreIfNull]
    public string? Label { get; set; }

    [BsonElement("url")]
    public string Url { get; set; } = string.Empty;

    [BsonElement("accessedAt")]
    [BsonIgnoreIfNull]
    public string? AccessedAt { get; set; }
}

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
