using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;

public sealed class SeoSitemapSnapshotDocument : MongoDocumentBase
{
    [BsonElement("generatedAtUtc")]
    public DateTime GeneratedAtUtc { get; set; }

    [BsonElement("publicBaseUrl")]
    public string PublicBaseUrl { get; set; } = string.Empty;

    [BsonElement("indexXml")]
    public string IndexXml { get; set; } = string.Empty;

    [BsonElement("sectionsStorageId")]
    [BsonIgnoreIfNull]
    public string? SectionsStorageId { get; set; }

    [BsonElement("sectionXmlByKey")]
    [BsonIgnoreIfNull]
    public Dictionary<string, string>? SectionXmlByKey { get; set; }

    [BsonElement("sections")]
    public List<SeoSitemapSectionStatsDocument> Sections { get; set; } = new List<SeoSitemapSectionStatsDocument>();

    [BsonElement("totalUrlCount")]
    public int TotalUrlCount { get; set; }
}
