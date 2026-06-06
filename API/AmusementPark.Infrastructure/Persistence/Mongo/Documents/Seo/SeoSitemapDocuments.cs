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

    [BsonElement("sectionXmlByKey")]
    public Dictionary<string, string> SectionXmlByKey { get; set; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    [BsonElement("sections")]
    public List<SeoSitemapSectionStatsDocument> Sections { get; set; } = new List<SeoSitemapSectionStatsDocument>();

    [BsonElement("totalUrlCount")]
    public int TotalUrlCount { get; set; }
}

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

public sealed class SeoSitemapSettingsDocument : MongoDocumentBase
{
    [BsonElement("isIndexNowEnabled")]
    public bool IsIndexNowEnabled { get; set; }

    [BsonElement("submitToIndexNowAfterManualGeneration")]
    public bool SubmitToIndexNowAfterManualGeneration { get; set; }

    [BsonElement("submitToIndexNowAfterAutomaticGeneration")]
    public bool SubmitToIndexNowAfterAutomaticGeneration { get; set; }

    [BsonElement("indexNowKey")]
    public string IndexNowKey { get; set; } = string.Empty;

    [BsonElement("indexNowKeyLocation")]
    public string IndexNowKeyLocation { get; set; } = string.Empty;

    [BsonElement("indexNowEndpoints")]
    public List<string> IndexNowEndpoints { get; set; } = new List<string>();
}

public sealed class SeoSitemapSectionStatsDocument
{
    [BsonElement("key")]
    public string Key { get; set; } = string.Empty;

    [BsonElement("fileName")]
    public string FileName { get; set; } = string.Empty;

    [BsonElement("displayName")]
    public string DisplayName { get; set; } = string.Empty;

    [BsonElement("urlCount")]
    public int UrlCount { get; set; }

    [BsonElement("lastModifiedUtc")]
    [BsonIgnoreIfNull]
    public DateTime? LastModifiedUtc { get; set; }
}

public sealed class SeoIndexNowSubmissionDocument
{
    [BsonElement("wasRequested")]
    public bool WasRequested { get; set; }

    [BsonElement("isEnabled")]
    public bool IsEnabled { get; set; }

    [BsonElement("isSuccess")]
    public bool IsSuccess { get; set; }

    [BsonElement("submittedUrlCount")]
    public int SubmittedUrlCount { get; set; }

    [BsonElement("acceptedEndpoints")]
    public List<string> AcceptedEndpoints { get; set; } = new List<string>();

    [BsonElement("errors")]
    public List<string> Errors { get; set; } = new List<string>();
}
