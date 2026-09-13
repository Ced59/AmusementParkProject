using AmusementPark.Infrastructure.Persistence.Mongo.Documents.Common;
using MongoDB.Bson.Serialization.Attributes;

namespace AmusementPark.Infrastructure.Persistence.Mongo.Documents.Seo;

public sealed class SeoSitemapGenerationHistoryDocument : MongoDocumentBase
{
    [BsonElement("startedAtUtc")]
    public DateTime StartedAtUtc { get; set; }

    [BsonElement("completedAtUtc")]
    [BsonIgnoreIfNull]
    public DateTime? CompletedAtUtc { get; set; }

    [BsonElement("durationMs")]
    public long DurationMs { get; set; }

    [BsonElement("status")]
    public string Status { get; set; } = string.Empty;

    [BsonElement("trigger")]
    public string Trigger { get; set; } = string.Empty;

    [BsonElement("triggeredByUserId")]
    [BsonIgnoreIfNull]
    public string? TriggeredByUserId { get; set; }

    [BsonElement("triggeredByUserEmail")]
    [BsonIgnoreIfNull]
    public string? TriggeredByUserEmail { get; set; }

    [BsonElement("totalUrlCount")]
    public int TotalUrlCount { get; set; }

    [BsonElement("sections")]
    public List<SeoSitemapSectionStatsDocument> Sections { get; set; } = new List<SeoSitemapSectionStatsDocument>();

    [BsonElement("errors")]
    public List<string> Errors { get; set; } = new List<string>();

    [BsonElement("indexNow")]
    public SeoIndexNowSubmissionDocument IndexNow { get; set; } = new SeoIndexNowSubmissionDocument();
}
