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
